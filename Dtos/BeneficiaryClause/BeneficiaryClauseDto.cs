using api.Dtos.Contract;

namespace api.Dtos.BeneficiaryClause
{
    public class BeneficiaryClauseDto
    {
        public int Id { get; set; }
        public string ClauseType { get; set; } = string.Empty;
        public bool Locked { get; set; }
        public int? ContractId { get; set; }
        public ContractDto? Contract { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RelationWithSubscriber { get; set; } = string.Empty;
        public List<BeneficiaryClausePersonDto> Beneficiaries { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class BeneficiaryClauseListItemDto
    {
        public int Id { get; set; }
        public string ClauseType { get; set; } = string.Empty;
        public bool Locked { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public List<BeneficiaryListPersonDto> Beneficiaries { get; set; } = new();
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class BeneficiaryListPersonDto
    {
        public int PersonId { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string RelationWithClause { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
    }

}
