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
        Type RendererType,
        DocumentRenderEngine RenderEngine = DocumentRenderEngine.QuestPdf,
        DocumentRenderOptions? RenderOptions = null)
    {
        public DocumentRenderOptions EffectiveRenderOptions =>
            (RenderOptions ?? DocumentRenderOptions.Default) with
            {
                PageSize = string.IsNullOrWhiteSpace(RenderOptions?.PageSize)
                    ? DefaultPageSize
                    : RenderOptions.PageSize,
                Orientation = string.IsNullOrWhiteSpace(RenderOptions?.Orientation)
                    ? DefaultOrientation
                    : RenderOptions.Orientation
            };
    }

    public enum DocumentRenderEngine
    {
        QuestPdf,
        HtmlToPdf,
        PdfTemplateOverlay,
        PdfMerge
    }

    public sealed record DocumentRenderOptions(
        string PageSize,
        string Orientation,
        decimal MarginTopMm,
        decimal MarginRightMm,
        decimal MarginBottomMm,
        decimal MarginLeftMm,
        bool PrintBackground,
        bool PreferCssPageSize,
        bool DisplayHeaderFooter,
        bool AllowExternalAssets,
        string? HeaderTemplate,
        string? FooterTemplate)
    {
        public static DocumentRenderOptions Default { get; } = new(
            "A4",
            "Portrait",
            12,
            12,
            12,
            12,
            PrintBackground: true,
            PreferCssPageSize: true,
            DisplayHeaderFooter: true,
            AllowExternalAssets: false,
            HeaderTemplate: null,
            FooterTemplate: null);
    }
}
