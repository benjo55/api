using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class Document
    {
        public int Id { get; set; }

        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // ex: PDF, JPG…
        public string Url { get; set; } = string.Empty; // lien vers le fichier (ex: S3, Azure…)

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // 🔗 Lien vers le contrat (si applicable)
        public int? ContractId { get; set; }
        [ForeignKey("ContractId")]
        public Contract? Contract { get; set; }
    }
}
