namespace api.Dtos.Insee
{
    public sealed record InseeCommuneDto
    {
        public string Code { get; init; } = "";

        public string Uri { get; init; } = "";

        public string Type { get; init; } = "";

        public string DateCreation { get; init; } = "";

        public string IntituleSansArticle { get; init; } = "";

        public string TypeArticle { get; init; } = "";

        public string Intitule { get; init; } = "";
    }
}
