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

            await _context.Compartments.AddAsync(model);
            await _context.SaveChangesAsync();

            _logger.LogInformation("🆕 Compartiment {Label} créé pour le contrat {ContractId}", model.Label, model.ContractId);
            return model;
        }

        // ==========================================================
        // 🔹 UPDATE
        // ==========================================================
        public async Task<Compartment?> UpdateAsync(int id, UpdateCompartmentRequestDto dto)
        {
            var existing = await _context.Compartments.FirstOrDefaultAsync(c => c.Id == id);

            if (existing == null)
            {
                _logger.LogWarning("⚠️ Compartiment {Id} introuvable pour mise à jour", id);
                return null;
            }

            // 🚫 Protection : le compartiment global ne peut pas être modifié
            if (existing.IsDefault)
                throw new InvalidOperationException("Le compartiment global ne peut pas être modifié.");

            existing.Label = dto.Label;
            existing.ManagementMode = dto.ManagementMode;
            existing.Notes = dto.Notes;
            existing.UpdatedDate = DateTime.UtcNow;

            // 🚫 On ne touche pas aux allocations FSA (gérées par les opérations)
            await _context.SaveChangesAsync();

            _logger.LogInformation("✏️ Compartiment {Id} mis à jour (Label={Label})", existing.Id, existing.Label);
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
                throw new InvalidOperationException("Impossible de renommer le compartiment global.");

            existing.Label = newLabel;
            existing.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("📝 Label du compartiment {Id} modifié en {Label}", existing.Id, newLabel);

            return existing;
        }

        // ==========================================================
        // 🔹 DELETE
        // ==========================================================
        public async Task<bool> DeleteAsync(int id)
        {
            var comp = await _context.Compartments.FirstOrDefaultAsync(c => c.Id == id);
            if (comp == null)
                throw new InvalidOperationException("Compartiment introuvable.");

            if (comp.IsDefault)
                throw new InvalidOperationException("Le compartiment global ne peut pas être supprimé.");

            // 🚫 On ne supprime pas directement les FSA liées : elles seront nettoyées par le recalcul global
            _context.Compartments.Remove(comp);
            await _context.SaveChangesAsync();

            _logger.LogInformation("🗑️ Compartiment {Id} supprimé pour le contrat {ContractId}", comp.Id, comp.ContractId);
            return true;
        }
    }
}
