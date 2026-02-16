namespace api.Dtos.Contract
{
    public class ContractOptionDto
    {
        public int Id { get; set; }  // Id de l’option activée
        public int ContractOptionTypeId { get; set; }  // FK vers le catalogue

        public bool IsActive { get; set; } = true;    // activée ou pas
        public string? Description { get; set; }      // note personnalisée
        public string? CustomParameters { get; set; } // JSON (seuil %, fréquence, etc.)

        // 🔹 Pour affichage UI (lazy load du catalogue)
        public ContractOptionTypeDto? OptionType { get; set; }
    }
}
