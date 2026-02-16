using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public int ContractCount { get; set; }
        public int? InsurerId { get; set; }
        [ForeignKey("InsurerId")]
        public Insurer? Insurer { get; set; }
        public bool Locked { get; set; } = false;
    }
}