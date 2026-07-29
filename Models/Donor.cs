using api.Models.Enum;

namespace api.Models
{
    public class Donor
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }
        public DonorType DonorType { get; set; } = DonorType.Individual;
        public string? Title { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressGeoJson { get; set; }
        public string? AddressLine2 { get; set; }
        public string? StreetNumber { get; set; }
        public string StreetName { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string CountryCode { get; set; } = "FR";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsArchived { get; set; }

        public ICollection<Donation> Donations { get; set; } = new List<Donation>();

        public string FullName =>
            DonorType == DonorType.Company
                ? CompanyName ?? string.Empty
                : string.Join(" ", new[] { FirstName, LastName }.Where(x => !string.IsNullOrWhiteSpace(x)));

        public string FullAddress =>
            string.Join(", ", new[]
            {
                AddressLine1,
                AddressLine2,
                $"{PostalCode} {City}".Trim(),
                CountryCode
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
