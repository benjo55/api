public class ContractSupportHoldingDto
{
    public int SupportId { get; set; }
    public string SupportLabel { get; set; } = "";
    public string ISIN { get; set; } = "";
    public decimal Vl { get; set; }
    public decimal Pru { get; set; }
    public decimal TotalShares { get; set; }
    public decimal TotalInvested { get; set; }

    // 👉 Ajouts importants
    public decimal CurrentValue { get; set; }      // VL × Parts
    public decimal PerformancePercent { get; set; } // Perf réelle
    public DateTime? LastUpdated { get; set; }
}
