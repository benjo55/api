using api.Dtos.BeneficiaryClause;
using api.Dtos.Contract;
using api.Models;

namespace api.Mappers
{
    public static class BeneficiaryClauseMapper
    {
        public static BeneficiaryClauseDto ToBeneficiaryClauseDto(this BeneficiaryClause clause)
        {
            return new BeneficiaryClauseDto
            {
                Id = clause.Id,
                ClauseType = clause.ClauseType,
                Locked = clause.Locked,
                Description = clause.Description,
                Status = clause.Status,
                ContractId = clause.ContractId,
                Contract = clause.Contract?.ToContractDto(),
                CreatedDate = clause.CreatedDate,
                UpdatedDate = clause.UpdatedDate,
                Beneficiaries = clause.Beneficiaries?.Select(b => b.ToBeneficiaryClausePersonDto()).ToList() ?? new()

            };
        }

        public static BeneficiaryClause ToBeneficiaryClauseFromCreateDto(this CreateBeneficiaryClauseRequestDto clauseDto)
        {
            return new BeneficiaryClause
            {
                ClauseType = clauseDto.ClauseType,
                Locked = clauseDto.Locked,
                Description = clauseDto.Description,
                Status = clauseDto.Status,
                ContractId = clauseDto.ContractId,
                CreatedDate = clauseDto.CreatedDate
            };
        }

        public static void ToBeneficiaryClauseFromUpdateDto(this UpdateBeneficiaryClauseRequestDto clauseDto, BeneficiaryClause clause)
        {
            clause.ClauseType = clauseDto.ClauseType;
            clause.Locked = clauseDto.Locked;
            clause.ContractId = clauseDto.ContractId;
            clause.Description = clauseDto.Description;
            clause.Status = clauseDto.Status;
            clause.UpdatedDate = clauseDto.UpdatedDate;
        }
    }
}
