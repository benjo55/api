namespace api.Models.Enum
{
    public enum ContractFamily
    {
        AssuranceVie = 0,
        Capitalisation = 1,
        PERIndividuel = 2,
        PERCollectif = 3,
        PERObligatoire = 4,
        Madelin = 5,
        Article83 = 6,
        PEA = 7,
        PrevoyanceCollective = 8,
        Dependance = 9,
        HommeClé = 10,
        Article39 = 11,
    }

    public enum ExitMode
    {
        Capital = 0,
        Rente = 1,
        Both = 2,
    }

    public static class ContractFamilyExtensions
    {
        public static string ToLabel(this ContractFamily family) => family switch
        {
            ContractFamily.AssuranceVie => "Assurance Vie",
            ContractFamily.Capitalisation => "Capitalisation",
            ContractFamily.PERIndividuel => "PER Individuel",
            ContractFamily.PERCollectif => "PER Collectif",
            ContractFamily.PERObligatoire => "PER Obligatoire",
            ContractFamily.Madelin => "Madelin",
            ContractFamily.Article83 => "Article 83",
            ContractFamily.PEA => "PEA",
            ContractFamily.PrevoyanceCollective => "Prévoyance Collective",
            ContractFamily.Dependance => "Dépendance",
            ContractFamily.HommeClé => "Homme Clé",
            ContractFamily.Article39 => "Article 39",
            _ => family.ToString(),
        };
    }
}
