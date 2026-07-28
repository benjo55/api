using api.Models.Enum;

namespace api.Dtos.LegalDocuments
{
    public sealed record DocumentRenderModel(
        int RevisionId,
        string Code,
        string Name,
        string Version,
        string ContentHash,
        DocumentLayoutModel Layout,
        IReadOnlyList<DocumentRenderNode> Nodes);

    public sealed record DocumentLayoutModel(
        string PageFormat,
        decimal MarginTopMm,
        decimal MarginRightMm,
        decimal MarginBottomMm,
        decimal MarginLeftMm,
        string Css,
        string? HeaderHtml,
        string? FooterHtml,
        int TemplateVersion);

    public sealed record DocumentRenderNode(
        int Id,
        string StableKey,
        DocumentNodeType Type,
        string? Number,
        string Title,
        string? ContentHtml,
        bool IncludeInTableOfContents,
        bool StartOnNewPage,
        bool KeepWithNext,
        IReadOnlyList<DocumentRenderNode> Children);
}
