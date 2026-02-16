namespace api.Models
{
    public class IdentityRelation
    {
        public int Id { get; set; }

        public int SourceIdentityId { get; set; }
        public Identity SourceIdentity { get; set; } = null!;

        public int TargetIdentityId { get; set; }
        public Identity TargetIdentity { get; set; } = null!;

        public string RelationType { get; set; } = "OTHER"; // Enum possible
    }
}
