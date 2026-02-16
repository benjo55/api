namespace api.Models
{
    public abstract class Identity
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty; // "INDIVIDUAL" ou "COMPANY"
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    }
}
