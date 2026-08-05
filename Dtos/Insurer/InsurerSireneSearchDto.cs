namespace api.Dtos.Insurer
{
    public sealed record InsurerSireneSearchDto
    {
        public string Siren { get; init; } = "";

        public string? HeadquartersSiret { get; init; }

        public string LegalName { get; init; } = "";

        public string? TradeName { get; init; }

        public string? Acronym { get; init; }

        public string? LegalForm { get; init; }

        public string? IncorporationDate { get; init; }

        public string IncorporationCountryCode { get; init; } = "FR";

        public string HomeCountryCode { get; init; } = "FR";

        public string? ApeNafCode { get; init; }

        public string? HeadquartersAddress { get; init; }

        public string? HeadQuarters { get; init; }

        public string? PostalCode { get; init; }

        public string? City { get; init; }

        public string? Latitude { get; init; }

        public string? Longitude { get; init; }

        public string? VatNumber { get; init; }

        public string IsActive { get; init; } = "En activité";

        public string DataSourceType { get; init; } = "FrenchBusinessRegister";

        public string SourceName { get; init; } = "API Sirene INSEE";

        public string SourceUrl { get; init; } = "";

        public string SourceReference { get; init; } = "";

        public string RetrievedAt { get; init; } = "";

        public string LastVerifiedAt { get; init; } = "";

        public string VerificationStatus { get; init; } = "ToReview";

        public string? DataQualityNotes { get; init; }
    }
}
