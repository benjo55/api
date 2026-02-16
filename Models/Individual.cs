namespace api.Models
{
    public class Individual : Identity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string BirthCountry { get; set; } = string.Empty;
        public string BirthCity { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string Sex { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email1 { get; set; } = string.Empty;
        public string Email2 { get; set; } = string.Empty;
        public string TaxAddress { get; set; } = string.Empty;
        public string PostalAddress { get; set; } = string.Empty;

        public List<BeneficiaryClausePerson> BeneficiaryClausePersons { get; set; } = new();
        public List<Contract> Contracts { get; set; } = new();
    }
}
