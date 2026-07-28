namespace api.Models
{
    public class ClauseDefinition
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public byte[] RowVersion { get; set; } = [];

        public ICollection<ClauseRevision> Revisions { get; set; } = new List<ClauseRevision>();
    }
}
