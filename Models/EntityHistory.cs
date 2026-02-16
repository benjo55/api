using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class EntityHistory
    {
        public int Id { get; set; }
        public string EntityName { get; set; } = string.Empty; //-- Nom de l'entité concernée (ex: "Person", "Contract")
        public int EntityId { get; set; }  //-- Identifiant de l'entité concernée (ex: "1", "2")
        public string PropertyName { get; set; } = string.Empty; //-- Nom de la propriété modifiée (ex: "FirstName", "LastName")
        public string? OldValue { get; set; } = string.Empty; //-- Valeur avant modification
        public string? NewValue { get; set; } = string.Empty; //-- Valeur après modification
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public string ModifiedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
    }
}