using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Notary
{
    public class CreateNotaryRequestDto
    {
        public string RaisonSociale { get; set; } = string.Empty;
        public string Adresse1 { get; set; } = string.Empty;
        public string Adresse2 { get; set; } = string.Empty;
        public string CodePostal { get; set; } = string.Empty;
        public string Ville { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string Fax { get; set; } = string.Empty;
        public string SiteWeb { get; set; } = string.Empty;
        public string? AdresseMail { get; set; }= string.Empty;
        public string? NomContactNotaire { get; set; }= string.Empty;
    }
}