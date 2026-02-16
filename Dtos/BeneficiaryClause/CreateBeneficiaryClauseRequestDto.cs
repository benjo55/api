using System;
using System.Collections.Generic;

namespace api.Dtos.BeneficiaryClause
{
    public class CreateBeneficiaryClauseRequestDto
    {
        public string ClauseType { get; set; } = string.Empty;
        public bool Locked { get; set; }
        public int ContractId { get; set; } // ✅ Seul `ContractId` est utilisé
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RelationWithSubscriber { get; set; } = string.Empty;
        public List<BeneficiaryClausePersonDto> Beneficiaries { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
