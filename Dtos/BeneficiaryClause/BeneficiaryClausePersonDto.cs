using api.Dtos.Person;
using System;

namespace api.Dtos.BeneficiaryClause
{
    public class BeneficiaryClausePersonDto
    {
        public int ClauseId { get; set; }
        public int PersonId { get; set; }
        public string RelationWithClause { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
        public PersonDto? Person { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }

    public class BeneficiaryClausePersonExportDto
    {
        public int ClauseId { get; set; }
        public int PersonId { get; set; }
        public decimal Percentage { get; set; }
    }

}
