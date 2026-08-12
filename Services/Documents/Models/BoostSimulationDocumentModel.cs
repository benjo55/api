namespace api.Services.Documents.Models
{
    public sealed record BoostSimulationDocumentModel(
        BoostCollecteModel Collecte,
        IReadOnlyList<BoostOperationModel> Operations,
        string FileName);

    public sealed record BoostCollecteModel(
        int Id,
        string DescriptionCollecte,
        decimal TauxCollecte1,
        decimal TauxCollecte2,
        string PrenomClient,
        string NomClient);

    public sealed record BoostOperationModel(
        int Id,
        string DescriptionOperation,
        DateTime DateOperation,
        decimal MontantOperation,
        string CategorieOperation,
        decimal EligibleS1,
        decimal EligibleS2,
        decimal MontantBoost);
}
