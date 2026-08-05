using api.Dtos.Insurer;
using api.Models;
using Mapster;

namespace api.Mappers
{
    public static class InsurerMapper

    {
        public static InsurerDto ToInsurerDto(this Insurer InsurerModel)
        {
            var dto = InsurerModel.Adapt<InsurerDto>();
            dto.AuthorizationCount = InsurerModel.AuthorizationCount != 0
                ? InsurerModel.AuthorizationCount
                : InsurerModel.Authorizations?.Count ?? 0;
            dto.ExerciseCountryCount = InsurerModel.ExerciseCountryCount;
            return dto;
        }
        public static Insurer ToInsurerFromCreateDto(this CreateInsurerRequestDto InsurerDto)

        {
            var insurer = InsurerDto.Adapt<Insurer>();
            ApplyInputNormalization(insurer);
            return insurer;
        }

        public static void ApplyInputNormalization(this Insurer insurer)
        {
            insurer.Name = string.IsNullOrWhiteSpace(insurer.Name)
                ? insurer.TradeName ?? insurer.LegalName ?? string.Empty
                : insurer.Name;
            insurer.Lei = insurer.Lei?.ToUpperInvariant();
            insurer.ParentLei = insurer.ParentLei?.ToUpperInvariant();
            insurer.UltimateParentLei = insurer.UltimateParentLei?.ToUpperInvariant();
            insurer.UpdatedDate ??= DateTime.UtcNow;
        }
    }
}
