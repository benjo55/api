namespace api.Dtos.Role
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string RoleCode { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public List<RolePermissionDto> RolePermissions { get; set; } = new();
    }

    public class RolePermissionDto
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public PermissionDto? Permission { get; set; }
    }

    public class PermissionDto
    {
        public int Id { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
    }
}
