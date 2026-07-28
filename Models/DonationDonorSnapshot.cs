namespace api.Models
{
    public class DonationDonorSnapshot
    {
        public int Id { get; set; }
        public int DonationId { get; set; }
        public Donation Donation { get; set; } = null!;
        public int? UserId { get; set; }
        public User? User { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = "FR";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}