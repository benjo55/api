using System.Linq;
using api.Dtos.Contract;
using api.Models;

namespace api.Mappers
{
    public static class ContractOptionMapper
    {
        // ---------- ContractOptionType (catalogue) ----------
        public static ContractOptionTypeDto ToDto(this ContractOptionType type)
        {
            return new ContractOptionTypeDto
            {
                Id = type.Id,
                Code = type.Code,
                Category = type.Category,
                Label = type.Label,
                Objective = type.Objective,
                Mechanism = type.Mechanism,
                DefaultCost = type.DefaultCost
            };
        }

        public static ContractOptionType ToModel(this ContractOptionTypeDto dto)
        {
            return new ContractOptionType
            {
                Id = dto.Id,
                Code = dto.Code,
                Category = dto.Category,
                Label = dto.Label,
                Objective = dto.Objective,
                Mechanism = dto.Mechanism,
                DefaultCost = dto.DefaultCost
            };
        }

        // ---------- ContractOption (activation) ----------
        public static ContractOptionDto ToDto(this ContractOption option)
        {
            return new ContractOptionDto
            {
                Id = option.Id,
                ContractOptionTypeId = option.ContractOptionTypeId,
                Description = option.Description,
                IsActive = option.IsActive,
                CustomParameters = option.CustomParameters,
                OptionType = option.ContractOptionType != null
                    ? option.ContractOptionType.ToDto() // 🔹 inclut le catalogue si chargé
                    : null
            };
        }

        public static ContractOption ToModel(this ContractOptionDto dto)
        {
            return new ContractOption
            {
                Id = dto.Id,
                ContractOptionTypeId = dto.ContractOptionTypeId,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CustomParameters = dto.CustomParameters,
                // ⚠️ On ne mappe pas OptionType → ContractOptionType pour éviter
                // d’attacher un doublon EF (on ne garde que l’ID)
            };
        }
    }
}
