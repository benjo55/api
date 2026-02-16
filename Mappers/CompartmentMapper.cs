using System.Linq;
using api.Dtos.Compartment;
using api.Dtos.FinancialSupport;
using api.Models;

namespace api.Mappers
{
    public static class CompartmentMapper
    {
        // 🔹 Model -> DTO
        public static CompartmentDto ToCompartmentDto(this Compartment model)
        {
            return new CompartmentDto
            {
                Id = model.Id,
                ContractId = model.ContractId,
                Label = model.Label,
                Description = model.Description,
                ManagementMode = model.ManagementMode,
                Notes = model.Notes,
                CreatedDate = model.CreatedDate,
                UpdatedDate = model.UpdatedDate,
                CurrentValue = model.CurrentValue,

                Supports = model.Supports?.Select(s => new FinancialSupportAllocationDto
                {
                    Id = s.Id,
                    SupportId = s.SupportId,
                    AllocationPercentage = s.AllocationPercentage,
                    Support = s.Support?.ToFinancialSupportDto(),
                    CreatedDate = s.CreatedDate,
                    UpdatedDate = s.UpdatedDate,

                    // ✅ Champs calculés
                    CurrentShares = s.CurrentShares,
                    CurrentAmount = s.CurrentAmount
                }).ToList() ?? new()
            };
        }

        // 🔹 DTO (Create) -> Model
        public static Compartment ToModel(this CreateCompartmentRequestDto dto)
        {
            return new Compartment
            {
                ContractId = dto.ContractId,
                Label = dto.Label,
                Description = dto.Description ?? string.Empty,
                ManagementMode = dto.ManagementMode,
                Notes = dto.Notes,

                // ✅ on ne gère plus les supports ici → initialisé vide
                Supports = new List<FinancialSupportAllocation>()
            };
        }
    }
}
