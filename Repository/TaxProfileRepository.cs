using api.Data;
using api.Dtos.TaxProfile;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class TaxProfileRepository : ITaxProfileRepository
    {
        private readonly ApplicationDBContext _context;

        public TaxProfileRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<List<TaxProfileDto>> GetAllAsync()
        {
            var profiles = await _context.TaxProfiles
                .OrderBy(p => p.ContractFamily)
                .ToListAsync();

            return profiles.Select(ToDto).ToList();
        }

        public async Task<TaxProfileDto?> GetByIdAsync(int id)
        {
            var profile = await _context.TaxProfiles.FindAsync(id);
            return profile == null ? null : ToDto(profile);
        }

        public async Task<TaxProfileDto?> GetByFamilyAsync(ContractFamily family)
        {
            var profile = await _context.TaxProfiles
                .FirstOrDefaultAsync(p => p.ContractFamily == family);
            return profile == null ? null : ToDto(profile);
        }

        public async Task<TaxProfile> CreateAsync(TaxProfile profile)
        {
            profile.CreatedDate = DateTime.UtcNow;
            _context.TaxProfiles.Add(profile);
            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<TaxProfile?> UpdateAsync(int id, UpdateTaxProfileDto dto)
        {
            var existing = await _context.TaxProfiles.FindAsync(id);
            if (existing == null) return null;
            if (existing.Locked) throw new InvalidOperationException("Ce profil fiscal est verrouillé et ne peut pas être modifié.");

            dto.Adapt(existing);
            existing.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _context.TaxProfiles.FindAsync(id);
            if (existing == null) return false;
            if (existing.Locked) throw new InvalidOperationException("Ce profil fiscal est verrouillé et ne peut pas être supprimé.");

            _context.TaxProfiles.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        private static TaxProfileDto ToDto(TaxProfile p)
        {
            var dto = p.Adapt<TaxProfileDto>();
            dto.ContractFamilyLabel = GetFamilyLabel(p.ContractFamily);
            return dto;
        }

        public static string GetFamilyLabel(ContractFamily family) => family switch
        {
            ContractFamily.AssuranceVie => "Assurance-vie",
            ContractFamily.Capitalisation => "Contrat de capitalisation",
            ContractFamily.PERIndividuel => "PER individuel (PERIN)",
            ContractFamily.PERCollectif => "PER collectif (PERCOL)",
            ContractFamily.PERObligatoire => "PER obligatoire (PERO)",
            ContractFamily.Madelin => "Contrat Madelin",
            ContractFamily.Article83 => "Article 83",
            ContractFamily.PEA => "PEA",
            ContractFamily.PrevoyanceCollective => "Prévoyance collective",
            ContractFamily.Dependance => "Contrat dépendance",
            ContractFamily.HommeClé => "Homme-clé / assurance-vie entreprise",
            ContractFamily.Article39 => "Article 39",
            _ => family.ToString(),
        };
    }
}
