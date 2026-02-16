namespace api.Models
{
    public class FieldDescription
    {
        public int Id { get; set; }
        public string EntityName { get; set; } = string.Empty;    // ex: "BeneficiaryClause"
        public string FieldName { get; set; } = string.Empty;  // ex: "clauseType"
        public string Description { get; set; } = string.Empty;  // HTML ou texte enrichi
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
    }
}
