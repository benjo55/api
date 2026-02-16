namespace api.Models
{
    public class Company : Identity
    {
        public string LegalName { get; set; } = string.Empty;
        public string Siret { get; set; } = string.Empty;
        public string VatNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
