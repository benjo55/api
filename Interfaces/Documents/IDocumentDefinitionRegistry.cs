namespace api.Interfaces.Documents
{
    public interface IDocumentDefinitionRegistry
    {
        DocumentDefinition? Find(string key);
        IReadOnlyCollection<DocumentDefinition> List();
    }

    public sealed record DocumentDefinition(
        string Key,
        string DisplayName,
        string TemplateVersion,
        string DefaultFileNamePattern,
        string DefaultPageSize,
        string DefaultOrientation,
        string? RequiredPermission,
        bool SupportsPreview,
        bool SupportsDownload,
        bool SupportsArchive,
        bool SupportsEmail,
        Type DataProviderType,
        Type RendererType);
}
