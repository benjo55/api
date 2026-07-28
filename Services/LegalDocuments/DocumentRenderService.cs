using System.Net;
using System.Text;
using api.Data;
using api.Dtos.LegalDocuments;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.LegalDocuments
{
    public sealed class DocumentRenderService : IDocumentRenderService
    {
        private const int RendererVersion = 3;
        private readonly ApplicationDBContext _db;
        private readonly IDocumentNumberingService _numberingService;
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly IDocumentBinaryStorage _storage;
        private readonly IDocumentAuditService _auditService;

        public DocumentRenderService(
            ApplicationDBContext db,
            IDocumentNumberingService numberingService,
            IPdfGenerationService pdfGenerationService,
            IDocumentBinaryStorage storage,
            IDocumentAuditService auditService)
        {
            _db = db;
            _numberingService = numberingService;
            _pdfGenerationService = pdfGenerationService;
            _storage = storage;
            _auditService = auditService;
        }

        public async Task<DocumentRenderModel> BuildRenderModelAsync(int revisionId, CancellationToken cancellationToken = default)
        {
            var revision = await _db.LegalDocumentRevisions
                .AsNoTracking()
                .Include(x => x.LegalDocumentDefinition)
                .Include(x => x.DocumentLayoutTemplate)
                .Include(x => x.Nodes)
                .FirstOrDefaultAsync(x => x.Id == revisionId, cancellationToken)
                ?? throw new KeyNotFoundException("Revision not found.");

            var layout = revision.DocumentLayoutTemplate ?? new DocumentLayoutTemplate();
            var numbers = _numberingService.GenerateNumbers(revision.Nodes);
            var nodes = BuildRenderTree(revision.Nodes, numbers);

            return new DocumentRenderModel(
                revision.Id,
                revision.LegalDocumentDefinition.Code,
                revision.LegalDocumentDefinition.Name,
                $"{revision.MajorVersion}.{revision.MinorVersion}",
                revision.ContentHash ?? string.Empty,
                new DocumentLayoutModel(
                    layout.PageFormat,
                    layout.MarginTopMm,
                    layout.MarginRightMm,
                    layout.MarginBottomMm,
                    layout.MarginLeftMm,
                    layout.Css,
                    layout.HeaderHtml,
                    layout.FooterHtml,
                    layout.TemplateVersion),
                nodes);
        }

        public string RenderCanonicalHtml(DocumentRenderModel model)
        {
            var builder = new StringBuilder();
            builder.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>");
            builder.Append(WebUtility.HtmlEncode(model.Name));
            builder.Append("</title><style>");
            builder.Append(DefaultCss(model.Layout));
            builder.Append(model.Layout.Css);
            builder.Append("</style></head><body>");
            builder.Append("<main class=\"document\">");
            builder.Append("<section class=\"cover\"><h1>");
            builder.Append(WebUtility.HtmlEncode(model.Name));
            builder.Append("</h1><p>");
            builder.Append(WebUtility.HtmlEncode(model.Code));
            builder.Append(" - version ");
            builder.Append(WebUtility.HtmlEncode(model.Version));
            builder.Append("</p></section>");

            var tocNodes = model.Nodes
                .SelectMany(node => FlattenWithDepth(node, 0))
                .Where(x => x.Node.IncludeInTableOfContents)
                .ToList();
            if (tocNodes.Count > 0)
            {
                builder.Append("<nav class=\"toc\" aria-label=\"Table des matières\"><div class=\"toc-heading\">");
                builder.Append("<span class=\"toc-kicker\">DOCUMENT</span><h2>Table des matières</h2>");
                builder.Append("<p>Accédez directement à chaque partie du document.</p></div><div class=\"toc-entries\">");
                foreach (var entry in tocNodes)
                {
                    var anchorId = NodeAnchorId(entry.Node.Id);
                    var level = Math.Clamp(entry.Depth, 0, 3);
                    builder.Append("<a class=\"toc-row toc-level-");
                    builder.Append(level);
                    builder.Append("\" href=\"#");
                    builder.Append(anchorId);
                    builder.Append("\"><span class=\"toc-label\">");
                    if (!string.IsNullOrWhiteSpace(entry.Node.Number))
                    {
                        builder.Append("<span class=\"toc-number\">");
                        builder.Append(WebUtility.HtmlEncode(entry.Node.Number));
                        builder.Append("</span>");
                    }

                    builder.Append("<span>");
                    builder.Append(WebUtility.HtmlEncode(entry.Node.Title));
                    builder.Append("</span></span><span class=\"toc-leader\" aria-hidden=\"true\"></span>");
                    builder.Append("<span class=\"toc-page\" data-toc-target=\"");
                    builder.Append(entry.Node.Id);
                    builder.Append("\" aria-label=\"Page\">000</span></a>");
                }

                builder.Append("</div></nav>");
            }

            foreach (var node in model.Nodes)
            {
                RenderNode(builder, node);
            }

            builder.Append("</main></body></html>");
            return builder.ToString();
        }

        public async Task<DocumentPreviewDto> GeneratePreviewAsync(int revisionId, string revisionStamp, string? userName, CancellationToken cancellationToken = default)
        {
            var model = await BuildRenderModelAsync(revisionId, cancellationToken);
            var cacheKey = BuildCacheKey(model, revisionStamp);
            var existing = await _db.DocumentArtifacts
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.LegalDocumentRevisionId == revisionId && x.Type == DocumentArtifactType.PreviewPdf && x.CacheKey == cacheKey, cancellationToken);

            if (existing is not null)
            {
                try
                {
                    await _storage.ReadAsync(existing.StorageKey, cancellationToken);
                    return new DocumentPreviewDto(existing.Id, existing.FileName, existing.ContentType, existing.Hash, revisionStamp, IsCurrent(model, revisionStamp));
                }
                catch
                {
                    // L'artefact peut survivre en base alors que le stockage local a ete purge lors d'un deploiement.
                    // On regenere plus bas au lieu de renvoyer un artifactId inutilisable.
                }
            }

            var html = RenderCanonicalHtml(model);
            var pdf = await _pdfGenerationService.GeneratePdfAsync(html, model.Layout.PageFormat, cancellationToken);
            var saved = await _storage.SaveAsync(pdf, ".pdf", cancellationToken);

            var artifact = new DocumentArtifact
            {
                Type = DocumentArtifactType.PreviewPdf,
                LegalDocumentRevisionId = revisionId,
                StorageKey = saved.StorageKey,
                ContentType = "application/pdf",
                FileName = $"{model.Code}-{model.Version}-preview.pdf",
                Hash = saved.Hash,
                Size = saved.Size,
                GeneratedBy = userName,
                CacheKey = cacheKey
            };

            _db.DocumentArtifacts.Add(artifact);
            await _db.SaveChangesAsync(cancellationToken);
            await _auditService.AddAsync(DocumentAuditAction.PreviewGenerated, null, revisionId, null, new { artifact.Id, artifact.Hash }, userName, cancellationToken);
            return new DocumentPreviewDto(artifact.Id, artifact.FileName, artifact.ContentType, artifact.Hash, revisionStamp, IsCurrent(model, revisionStamp));
        }

        private static IReadOnlyList<DocumentRenderNode> BuildRenderTree(IEnumerable<LegalDocumentNode> nodes, IReadOnlyDictionary<int, string> numbers)
        {
            var list = nodes.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToList();
            var byParent = list.GroupBy(x => x.ParentNodeId ?? 0).ToDictionary(x => x.Key, x => x.ToList());

            IReadOnlyList<DocumentRenderNode> Build(int? parentId)
            {
                if (!byParent.TryGetValue(parentId ?? 0, out var children))
                {
                    return [];
                }

                return children
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Id)
                    .Select(x => new DocumentRenderNode(
                        x.Id,
                        x.StableKey,
                        x.Type,
                        numbers.TryGetValue(x.Id, out var number) ? number : null,
                        x.Title,
                        x.ContentHtml,
                        x.IncludeInTableOfContents,
                        x.StartOnNewPage,
                        x.KeepWithNext,
                        Build(x.Id)))
                    .ToList();
            }

            return Build(null);
        }

        private static void RenderNode(StringBuilder builder, DocumentRenderNode node)
        {
            var classes = new List<string> { "node", $"node-{node.Type.ToString().ToLowerInvariant()}" };
            if (node.StartOnNewPage)
            {
                classes.Add("start-new-page");
            }

            if (node.KeepWithNext)
            {
                classes.Add("keep-with-next");
            }

            builder.Append("<section class=\"");
            builder.Append(string.Join(" ", classes));
            builder.Append("\" id=\"");
            builder.Append(NodeAnchorId(node.Id));
            builder.Append("\" data-stable-key=\"");
            builder.Append(WebUtility.HtmlEncode(node.StableKey));
            builder.Append("\"><span class=\"pdf-page-marker\" aria-hidden=\"true\">[[PDF_TARGET_");
            builder.Append(node.Id);
            builder.Append("]]</span>");

            if (
                node.Type != DocumentNodeType.Document &&
                node.Type != DocumentNodeType.Paragraph &&
                node.Type != DocumentNodeType.PageBreak)
            {
                builder.Append("<h");
                builder.Append(HeadingLevel(node.Type));
                builder.Append(">");
                if (!string.IsNullOrWhiteSpace(node.Number))
                {
                    builder.Append("<span class=\"number\">");
                    builder.Append(WebUtility.HtmlEncode(node.Number));
                    builder.Append("</span> ");
                }

                builder.Append(WebUtility.HtmlEncode(node.Title));
                builder.Append("</h");
                builder.Append(HeadingLevel(node.Type));
                builder.Append(">");
            }

            if (node.Type == DocumentNodeType.PageBreak)
            {
                builder.Append("<div class=\"page-break\"></div>");
            }
            else if (!string.IsNullOrWhiteSpace(node.ContentHtml))
            {
                builder.Append("<div class=\"content\">");
                builder.Append(node.ContentHtml);
                builder.Append("</div>");
            }

            foreach (var child in node.Children)
            {
                RenderNode(builder, child);
            }

            builder.Append("</section>");
        }

        private static int HeadingLevel(DocumentNodeType type) => type switch
        {
            DocumentNodeType.Part => 1,
            DocumentNodeType.Title => 1,
            DocumentNodeType.Chapter => 2,
            DocumentNodeType.Section => 3,
            DocumentNodeType.Article => 4,
            _ => 5
        };

        private static string DefaultCss(DocumentLayoutModel layout)
        {
            static string Mm(decimal value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            return string.Join(Environment.NewLine,
                "@page {",
                $"  size: {layout.PageFormat};",
                $"  margin: {Mm(layout.MarginTopMm)}mm {Mm(layout.MarginRightMm)}mm {Mm(layout.MarginBottomMm)}mm {Mm(layout.MarginLeftMm)}mm;",
                "}",
                "body { font-family: Arial, Helvetica, sans-serif; color: #1f2933; font-size: 11pt; line-height: 1.45; }",
                ".cover { page-break-after: always; min-height: 80vh; display: flex; flex-direction: column; justify-content: center; }",
                ".cover h1 { font-size: 28pt; margin: 0 0 14mm; }",
                ".toc { page-break-after: always; color: #1f2933; }",
                ".toc-heading { margin-bottom: 10mm; padding-bottom: 5mm; border-bottom: 2px solid #2b6cb0; }",
                ".toc-kicker { display: block; margin-bottom: 2mm; color: #2b6cb0; font-size: 8pt; font-weight: 700; letter-spacing: 1.4px; }",
                ".toc h2 { margin: 0; color: #12263a; font-size: 22pt; font-weight: 700; }",
                ".toc-heading p { margin: 2mm 0 0; color: #64748b; font-size: 9pt; }",
                ".toc-entries { display: flex; flex-direction: column; gap: 0; }",
                ".toc-row { display: flex; align-items: baseline; gap: 2.5mm; min-height: 7mm; padding: 1.6mm 0; color: #26384a; text-decoration: none; break-inside: avoid; }",
                ".toc-row:hover { color: #2b6cb0; }",
                ".toc-label { display: inline-flex; gap: 2mm; max-width: 78%; }",
                ".toc-number { min-width: 12mm; color: #2b6cb0; font-weight: 700; }",
                ".toc-leader { flex: 1; min-width: 8mm; border-bottom: 1px dotted #94a3b8; transform: translateY(-1.2mm); }",
                ".toc-page { min-width: 9mm; color: #12263a; font-variant-numeric: tabular-nums; font-weight: 700; text-align: right; }",
                ".toc-level-0 { font-size: 10.5pt; font-weight: 700; }",
                ".toc-level-1 { padding-left: 5mm; font-size: 10pt; }",
                ".toc-level-2 { padding-left: 10mm; color: #475569; font-size: 9.5pt; }",
                ".toc-level-3 { padding-left: 15mm; color: #64748b; font-size: 9pt; }",
                ".node { position: relative; }",
                ".pdf-page-marker { position: absolute; color: #fff; font-size: 1px; line-height: 1; white-space: nowrap; }",
                "h1, h2, h3, h4, h5 { page-break-after: avoid; color: #12263a; }",
                "table { width: 100%; border-collapse: collapse; page-break-inside: avoid; }",
                "th, td { border: 1px solid #d0d7de; padding: 4px 6px; }",
                ".start-new-page { page-break-before: always; }",
                ".keep-with-next { page-break-after: avoid; }",
                ".page-break { page-break-before: always; }",
                ".number { color: #52606d; }");
        }

        private static IEnumerable<(DocumentRenderNode Node, int Depth)> FlattenWithDepth(
            DocumentRenderNode node,
            int depth)
        {
            yield return (node, depth);
            foreach (var child in node.Children)
            {
                foreach (var descendant in FlattenWithDepth(child, depth + 1))
                {
                    yield return descendant;
                }
            }
        }

        private static string NodeAnchorId(int nodeId) => $"document-node-{nodeId}";

        private static string BuildCacheKey(DocumentRenderModel model, string revisionStamp)
        {
            var input = $"{RendererVersion}|{model.RevisionId}|{model.ContentHash}|{model.Layout.TemplateVersion}|{revisionStamp}";
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(input)));
        }

        private static bool IsCurrent(DocumentRenderModel model, string revisionStamp) =>
            string.IsNullOrWhiteSpace(revisionStamp) || revisionStamp == model.ContentHash;
    }
}
