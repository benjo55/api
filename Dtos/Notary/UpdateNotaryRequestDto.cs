using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Notary
{
    public class UpdateNotaryRequestDto
    {

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