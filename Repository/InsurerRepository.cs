using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Insurer;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Mappers;
using api.Models;
using api.Services;
using Mapster;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace api.Repository
{
    public class InsurerRepository : IInsurerRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly EntityHistoryService _entityHistoryService;  // Service d'historisation
        public InsurerRepository(ApplicationDBContext context, EntityHistoryService entityHistoryService)
        {
            _context = context;
            _entityHistoryService = entityHistoryService;
        }

        public async Task<Insurer> CreateAsync(Insurer InsurerModel)
        {
            EnsureLegacyContactPoints(InsurerModel);
            await _context.Insurers.AddAsync(InsurerModel);
            await _context.SaveChangesAsync();
            await PopulateRelationCountsAsync(InsurerModel);
            return InsurerModel;
        }

        public async Task<Insurer?> DeleteAsync(int id)
        {
            try
            {
                var InsurerModel = await _context.Insurers.FirstOrDefaultAsync(p => p.Id == id);
                if (InsurerModel == null) return null;
                _context.Insurers.Remove(InsurerModel);
                await _context.SaveChangesAsync();
                return InsurerModel;
            }
            catch (DbUpdateException ex) when ((ex.InnerException as SqlException)?.Number == 547)
            {
                throw new InvalidOperationException("Impossible de supprimer cet assureur car il est référencé par un ou plusieurs produits.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Une erreur inattendue s'est produite lors de la suppression.", ex);
            }

        }

        public async Task<PagedResult<Insurer>> GetAllAsync(QueryObject query)
        {
            var Insurers = _context.Insurers.AsQueryable();
            Insurers = Insurers.OrderByDescending(p => p.CreatedDate);

            // Calcul du total avant pagination
            var totalCount = await Insurers.CountAsync();

            // Calcul du nombre total de pages
            var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);

            // Pagination
            var skipNumber = (query.PageNumber - 1) * query.PageSize;
            var pagedInsurers = await Insurers.Skip(skipNumber).Take(query.PageSize).ToListAsync();
            foreach (var insurer in pagedInsurers)
            {
                await PopulateRelationCountsAsync(insurer);
            }

            // Indique s'il reste une page après celle-ci
            var hasNextPage = query.PageNumber < totalPages;

            // Retour des résultats
            return new PagedResult<Insurer>
            {
                Items = pagedInsurers,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = hasNextPage,
                CurrentPage = query.PageNumber
            };
        }
        public async Task<Insurer?> GetByIdAsync(int id)
        {
            var insurer = await _context.Insurers
                .Include(p => p.Authorizations)
                .Include(p => p.ContactPoints)
                .Include(p => p.SolvencyMetrics)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (insurer == null) return null;

            await PopulateRelationCountsAsync(insurer);
            return insurer;
        }

        public async Task<bool> InsurerExists(int id)
        {
            return await _context.Insurers.AnyAsync(p => p.Id == id);
        }
        public async Task<Insurer?> UpdateAsync(int id, UpdateInsurerRequestDto updateInsurerDto)
        {
            var existingInsurer = await _context.Insurers
                .Include(p => p.Authorizations)
                .Include(p => p.ContactPoints)
                .Include(p => p.SolvencyMetrics)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (existingInsurer == null) return null;

            // 1️⃣ Cloner l'état initial pour l'historisation
            var originalInsurer = (Insurer)_context.Entry(existingInsurer).CurrentValues.ToObject();

            // 2️⃣ Mise à jour avec Mapster
            updateInsurerDto.Adapt(existingInsurer);
            existingInsurer.Authorizations.Clear();
            existingInsurer.ContactPoints.Clear();
            existingInsurer.SolvencyMetrics.Clear();

            foreach (var authorization in updateInsurerDto.Authorizations.Adapt<List<InsurerAuthorization>>())
            {
                authorization.Id = 0;
                authorization.InsurerId = id;
                existingInsurer.Authorizations.Add(authorization);
            }

            foreach (var contactPoint in updateInsurerDto.ContactPoints.Adapt<List<InsurerContactPoint>>())
            {
                contactPoint.Id = 0;
                contactPoint.InsurerId = id;
                existingInsurer.ContactPoints.Add(contactPoint);
            }

            foreach (var solvencyMetric in updateInsurerDto.SolvencyMetrics.Adapt<List<InsurerSolvencyMetric>>())
            {
                solvencyMetric.Id = 0;
                solvencyMetric.InsurerId = id;
                existingInsurer.SolvencyMetrics.Add(solvencyMetric);
            }

            existingInsurer.ApplyInputNormalization();
            existingInsurer.UpdatedDate = DateTime.UtcNow;

            // 3️⃣ Historisation des changements
            await _entityHistoryService.TrackChangesAsync(originalInsurer, existingInsurer, "Admin"); // Remplace "Admin" par l'utilisateur courant

            // 4️⃣ Sauvegarde
            await _context.SaveChangesAsync();
            await PopulateRelationCountsAsync(existingInsurer);
            return existingInsurer;
        }

        public async Task<Insurer?> PatchLockedAsync(int id, bool locked)
        {
            var insurer = await _context.Insurers.FirstOrDefaultAsync(p => p.Id == id);
            if (insurer == null) return null;

            insurer.Locked = locked;
            await _context.SaveChangesAsync();
            return insurer;
        }

        private static void EnsureLegacyContactPoints(Insurer insurer)
        {
            if (insurer.ContactPoints.Count > 0) return;

            if (!string.IsNullOrWhiteSpace(insurer.HeadQuarters))
            {
                insurer.ContactPoints.Add(new InsurerContactPoint
                {
                    ContactType = "RegisteredOffice",
                    Label = "Siège social",
                    AddressLine1 = insurer.HeadQuarters,
                    Phone = insurer.PhoneNumber,
                    Email = insurer.Email,
                    WebsiteUrl = insurer.WebSite,
                    IsPrimary = true
                });
            }

            if (!string.IsNullOrWhiteSpace(insurer.PostalAddress) && insurer.PostalAddress != insurer.HeadQuarters)
            {
                insurer.ContactPoints.Add(new InsurerContactPoint
                {
                    ContactType = "PostalAddress",
                    Label = "Adresse postale",
                    AddressLine1 = insurer.PostalAddress,
                    Phone = string.IsNullOrWhiteSpace(insurer.HeadQuarters) ? insurer.PhoneNumber : null,
                    Email = string.IsNullOrWhiteSpace(insurer.HeadQuarters) ? insurer.Email : null,
                    WebsiteUrl = string.IsNullOrWhiteSpace(insurer.HeadQuarters) ? insurer.WebSite : null,
                    IsPrimary = true
                });
            }

            if (insurer.ContactPoints.Count == 0 && (!string.IsNullOrWhiteSpace(insurer.PhoneNumber) || !string.IsNullOrWhiteSpace(insurer.Email) || !string.IsNullOrWhiteSpace(insurer.WebSite)))
            {
                insurer.ContactPoints.Add(new InsurerContactPoint
                {
                    ContactType = "CustomerService",
                    Label = "Contact principal",
                    Phone = insurer.PhoneNumber,
                    Email = insurer.Email,
                    WebsiteUrl = insurer.WebSite,
                    IsPrimary = true
                });
            }
        }

        private async Task PopulateRelationCountsAsync(Insurer insurer)
        {
            var productIds = await _context.Products
                .Where(p => p.InsurerId == insurer.Id)
                .Select(p => p.Id)
                .ToListAsync();
            var contractIds = productIds.Count == 0
                ? new List<int>()
                : await _context.Contracts
                    .Where(c => c.ProductId.HasValue && productIds.Contains(c.ProductId.Value))
                    .Select(c => c.Id)
                    .ToListAsync();

            insurer.ProductCount = productIds.Count;
            insurer.ContractCount = contractIds.Count;
            insurer.DocumentCount = contractIds.Count == 0
                ? 0
                : await _context.Documents.CountAsync(d => d.ContractId.HasValue && contractIds.Contains(d.ContractId.Value));
            insurer.PersonCount = contractIds.Count == 0
                ? 0
                : await _context.Contracts
                    .Where(c => contractIds.Contains(c.Id) && c.PersonId.HasValue)
                    .Select(c => c.PersonId!.Value)
                    .Distinct()
                    .CountAsync();
            insurer.BrandCount = 0;
            insurer.AuthorizationCount = await _context.InsurerAuthorizations
                .CountAsync(a => a.InsurerId == insurer.Id);
            insurer.ExerciseCountryCount = await _context.InsurerAuthorizations
                .Where(a => a.InsurerId == insurer.Id && a.HostCountryCode != null && a.HostCountryCode != "")
                .Select(a => a.HostCountryCode)
                .Distinct()
                .CountAsync();
        }

    }
}
