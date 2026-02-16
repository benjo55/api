using api.Models;

public class ContractOptionType
{
    public int Id { get; set; }  // PK
    public string Code { get; set; } = string.Empty; // ex: "STOP_LOSS_GAIN"
    public string Category { get; set; } = string.Empty; // Gestion financière / Garanties / Autres
    public string Label { get; set; } = string.Empty;    // Libellé affiché
    public string Objective { get; set; } = string.Empty; // Objectif
    public string Mechanism { get; set; } = string.Empty; // Fonctionnement
    public string DefaultCost { get; set; } = string.Empty; // Coût par défaut

    // Navigation
    public ICollection<ContractOption> ContractOptions { get; set; } = new List<ContractOption>();
}
