namespace api.Services.Documents.Models
{
    public sealed record InformationSystemCartographyDocumentModel(
        string EmployerEntity,
        DateTime AsOfDate,
        string Classification,
        IReadOnlyList<CartographyDomainSectionModel> Sections,
        IReadOnlyList<CartographyApplicationModel> Applications,
        IReadOnlyList<CartographyConfigurationItemModel> ConfigurationItems,
        IReadOnlyList<CartographyFlowModel> Flows);

    public sealed record CartographyDomainSectionModel(
        string Title,
        int HeadingLevel,
        int SortOrder,
        string Content);

    public sealed record CartographyApplicationModel(
        int Id,
        string ExternalCiNumber,
        string Name,
        string? Domain,
        string? Owner,
        string? Criticality,
        string? Description,
        string? HostingMode);

    public sealed record CartographyConfigurationItemModel(
        int Id,
        string ExternalCiNumber,
        string Name,
        string? Category,
        string? Model,
        string? Status,
        string? OwnerName);

    public sealed record CartographyFlowModel(
        string SourceName,
        string TargetName,
        string Name,
        string PatternName,
        string InteractionMode,
        string? TechnologyName);
}
