using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class Person
    {
        public int Id { get; set; }
        [MaxLength(200)] // ✅ Taille fixée
        public string FirstName { get; set; } = string.Empty;
        [MaxLength(200)]
        public string LastName { get; set; } = string.Empty;
        public string BirthCountry { get; set; } = string.Empty;
        public string BirthCity { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; } = DateTime.Now;
        public string Sex { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        [MaxLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;
        [MaxLength(30)]
        [EmailAddress] // ✅ Validation d'email
        public string Email1 { get; set; } = string.Empty;
        [MaxLength(30)]
        [EmailAddress] // ✅ Validation d'email
        public string Email2 { get; set; } = string.Empty;
        public string TaxAddress { get; set; } = string.Empty;
        public string PostalAddress { get; set; } = string.Empty;
        // 🔹 Liste des clauses où cette personne est bénéficiaire
        public List<BeneficiaryClausePerson> BeneficiaryClausePersons { get; set; } = new();
        public List<Contract> Contracts { get; set; } = new List<Contract>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public bool Locked { get; set; } = false;
    }
}