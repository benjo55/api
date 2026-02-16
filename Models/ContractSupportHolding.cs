// Models/ContractSupportHolding.cs
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace api.Models
{

    public class ContractSupportHolding
    {
        public int Id { get; set; }

        public int ContractId { get; set; }
        public Contract Contract { get; set; } = null!;

        public int SupportId { get; set; }
        public FinancialSupport Support { get; set; } = null!;

        [Column(TypeName = "decimal(20,7)")]
        public decimal TotalShares { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal TotalInvested { get; set; }

        [Column(TypeName = "decimal(20,7)")]
        public decimal Pru { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(20,7)")]
        public decimal? CurrentAmount { get; set; }       // Valeur actuelle (VL × parts)
        [Precision(18, 4)]
        public decimal? PerformancePercent { get; set; }  // Perf instantanée en %

    }

}
