using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

// Attention Notary est une instance de Person

namespace api.Models
{
    public class Notary
    {
        public int Id { get; set; }

        public string? RaisonSociale { get; set; }

        public string? Adresse1 { get; set; }

        public string? Adresse2 { get; set; }

        public string? CodePostal { get; set; }

        public string? Ville { get; set; }

        public string? Telephone { get; set; }

        public string? Fax { get; set; }

        public string? SiteWeb { get; set; }

        public string? AdresseMail { get; set; }

        public string? NomContactNotaire { get; set; }
    }
}