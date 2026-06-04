using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Compartment;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace api.Repository
{
    public class CompartmentRepository : ICompartmentRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<CompartmentRepository> _logger;

        public CompartmentRepository(ApplicationDBContext context, ILogger<CompartmentRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================================
        // 🔹 GET BY CONTRACT
        // ==========================================================
        public async Task<List<Compartment>> GetByContractAsync(int contractId)
        {
            return await _context.Compartments
                .Include(c => c.Supports)
                    .ThenInclude(s => s.Support)
                .Where(c => c.ContractId == contractId)
                .OrderBy(c => c.Label)
                .ToListAsync();
        }

        // ==========================================================
        // 🔹 GET BY ID
        // ==========================================================
        public async Task<Compartment?> GetByIdAsync(int id)
        {
            return await _context.Compartments
                .Include(c => c.Supports)
                    .ThenInclude(s => s.Support)
                .Include(c => c.Contract)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // ==========================================================
        // 🔹 CREATE
        // ==========================================================
        public async Task<Compartment> CreateAsync(Compartment model, CreateCompartmentRequestDto dto)
        {
            // 🚫 Les supports (FSA) sont désormais gérés par les opérations.
            model.Supports = new List<FinancialSupportAllocation>();
            model.CreatedDate = DateTime.UtcNow;
            model.UpdatedDate = DateTime.UtcNow;
            model.Label = string.IsNullOrWhiteSpace(model.Label) ? "Poche" : model.Label.Trim();

            await _context.Compartments.AddAsync(model);
            await _context.SaveChangesAsync();

            _logger.LogInformation("🆕 Poche {Label} créé pour le contrat {ContractId}", model.Label, model.ContractId);
            return model;
        }

        // ==========================================================
        // 🔹 UPDATE
        // ==========================================================
        public async Task<Compartment?> UpdateAsync(int id, UpdateCompartmentRequestDto dto)
        {
            var existing = await _context.Compartments
                .Include(c => c.Contract)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existing == null)
            {
                _logger.LogWarning("⚠️ Poche {Id} introuvable pour mise à jour", id);
                return null;
            }

            // 🚫 Protection : la poche globale ne peut pas être modifiée
            if (existing.IsDefault)
                throw new InvalidOperationException("La poche globale ne peut pas être modifiée.");

            var restricted = await IsLockedContractWithOperationsAsync(existing.ContractId);

            if (restricted)
            {
                var managementChanged = !string.Equals(existing.ManagementMode ?? string.Empty, dto.ManagementMode ?? string.Empty, StringComparison.Ordinal);
                var notesChanged = !string.Equals(existing.Notes ?? string.Empty, dto.Notes ?? string.Empty, StringComparison.Ordinal);
                var descriptionChanged = !string.Equals(existing.Description ?? string.Empty, dto.Description ?? string.Empty, StringComparison.Ordinal);

                if (managementChanged || notesChanged || descriptionChanged)
                {
                    throw new InvalidOperationException("Ce contrat est verrouillé avec opérations: seul le renommage des poches est autorisé.");
                }
            }

            existing.Label = string.IsNullOrWhiteSpace(dto.Label)
                ? existing.Label
                : dto.Label.Trim();

            if (!restricted)
            {
                existing.ManagementMode = dto.ManagementMode;
                existing.Notes = dto.Notes;
                existing.Description = dto.Description ?? string.Empty;
            }

            existing.UpdatedDate = DateTime.UtcNow;

            // 🚫 On ne touche pas aux allocations FSA (gérées par les opérations)
            await _context.SaveChangesAsync();

            _logger.LogInformation("✏️ Poche {Id} mise à jour (Label={Label})", existing.Id, existing.Label);
            return existing;
        }

        // ==========================================================
        // 🔹 PATCH LABEL (renommage rapide)
        // ==========================================================
        public async Task<Compartment?> PatchLabelAsync(int id, string newLabel)
        {
            var existing = await _context.Compartments.FirstOrDefaultAsync(c => c.Id == id);
            if (existing == null) return null;

            if (existing.IsDefault)
                throw new InvalidOperationException("Impossible de renommer la poche globale.");

            existing.Label = string.IsNullOrWhiteSpace(newLabel) ? existing.Label : newLabel.Trim();
            existing.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("📝 Label de la poche {Id} modifié en {Label}", existing.Id, newLabel);

            return existing;
        }

        // ==========================================================
        // 🔹 DELETE
        // ==========================================================
        public async Task<bool> DeleteAsync(int id)
        {
            var comp = await _context.Compartments.FirstOrDefaultAsync(c => c.Id == id);
            if (comp == null)
                throw new InvalidOperationException("Poche introuvable.");

            if (comp.IsDefault)
                throw new InvalidOperationException("La poche globale ne peut pas être supprimée.");

            var restricted = await IsLockedContractWithOperationsAsync(comp.ContractId);
            if (restricted)
            {
                var invested = await IsCompartmentInvestedAsync(comp.ContractId, comp.Id);
                if (invested)
                {
                    throw new InvalidOperationException($"La poche '{comp.Label}' est investi et ne peut pas être supprimé.");
                }
            }

            // 🚫 On ne supprime pas directement les FSA liées : elles seront nettoyées par le recalcul global
            _context.Compartments.Remove(comp);
            await _context.SaveChangesAsync();

            _logger.LogInformation("🗑️ Poche {Id} supprimé pour le contrat {ContractId}", comp.Id, comp.ContractId);
            return true;
        }

        private async Task<bool> IsLockedContractWithOperationsAsync(int contractId)
        {
            var isLocked = await _context.Contracts
                .Where(c => c.Id == contractId)
                .Select(c => c.Locked)
                .FirstOrDefaultAsync();

            if (!isLocked)
                return false;

            return await _context.Operations.AnyAsync(o => o.ContractId == contractId);
        }

        private async Task<bool> IsCompartmentInvestedAsync(int contractId, int compartmentId)
        {
            var hasHoldings = await _context.ContractSupportHoldings.AnyAsync(h =>
                h.ContractId == contractId &&
                h.CompartmentId == compartmentId &&
                (h.TotalShares > 0m || h.TotalInvested > 0m || (h.CurrentAmount ?? 0m) > 0m));

            if (hasHoldings)
                return true;

            return await _context.FinancialSupportAllocations.AnyAsync(a =>
                a.ContractId == contractId &&
                a.CompartmentId == compartmentId &&
                (a.CurrentShares > 0m || a.InvestedAmount > 0m || a.CurrentAmount > 0m));
        }
    }
}
