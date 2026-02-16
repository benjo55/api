using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Models;

namespace api.Dtos.Insurer
{
    public class InsurerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RegistrationNumber { get; set; } = string.Empty;
        public int FoundedYear { get; set; }
        public string? HeadQuarters { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? PostalAddress { get; set; }
        public string? WebSite { get; set; }
        public string? IsActive { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public bool Locked { get; set; } = false; // Default to false, can be set to true when creating or updating an insurer
    }
}