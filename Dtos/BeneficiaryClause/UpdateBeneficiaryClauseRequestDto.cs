using System;
using System.Collections.Generic;
using api.Dtos.BeneficiaryClause;

namespace api.Dtos.BeneficiaryClause
{
    public class UpdateBeneficiaryClauseRequestDto
    {
        public string ClauseType { get; set; } = string.Empty;
        public bool Locked { get; set; }
        public int ContractId { get; set; } // ✅ Seul `ContractId` est utilisé
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RelationWithSubscriber { get; set; } = string.Empty;
        public List<BeneficiaryClausePersonDto> Beneficiaries { get; set; } = new List<BeneficiaryClausePersonDto>();
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}
