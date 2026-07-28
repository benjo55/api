using api.Dtos.LegalDocuments;
using api.Models;

namespace api.Services.LegalDocuments
{
    internal static class LegalDocumentMapping
    {
        public static string ToRowVersion(byte[] rowVersion) =>
            rowVersion.Length == 0 ? string.Empty : Convert.ToBase64String(rowVersion);

        public static byte[] FromRowVersion(string? rowVersion) =>
            string.IsNullOrWhiteSpace(rowVersion) ? [] : Convert.FromBase64String(rowVersion);

        public static LegalDocumentDefinitionListDto ToListDto(LegalDocumentDefinition definition) =>
            new(
                definition.Id,
                definition.Code,
                definition.Name,
                definition.Description,
                definition.Type,
                definition.IsLibrary,
                definition.CurrentDraftRevisionId,
                definition.CurrentPublishedRevisionId,
                definition.IsActive,
                ToRowVersion(definition.RowVersion));

        public static LegalDocumentDefinitionDto ToDto(LegalDocumentDefinition definition) =>
            new(
                definition.Id,
                definition.Code,
                definition.Name,
                definition.Description,
                definition.Type,
                definition.IsLibrary,
                definition.CurrentDraftRevisionId,
                definition.CurrentPublishedRevisionId,
                definition.IsActive,
                ToRowVersion(definition.RowVersion),
                definition.Revisions
                    .OrderByDescending(x => x.MajorVersion)
                    .ThenByDescending(x => x.MinorVersion)
                    .Select(ToSummaryDto)
                    .ToList());

        public static LegalDocumentRevisionSummaryDto ToSummaryDto(LegalDocumentRevision revision) =>
            new(
                revision.Id,
                revision.LegalDocumentDefinitionId,
                revision.MajorVersion,
                revision.MinorVersion,
                revision.Status,
                revision.ChangeSummary,
                revision.CreatedAt,
                revision.ValidatedAt,
                revision.PublishedAt,
                ToRowVersion(revision.RowVersion));

        public static LegalDocumentRevisionDto ToDto(LegalDocumentRevision revision, IReadOnlyDictionary<int, string>? numbers = null) =>
            new(
                revision.Id,
                revision.LegalDocumentDefinitionId,
                revision.BasedOnRevisionId,
                revision.MajorVersion,
                revision.MinorVersion,
                revision.Status,
                revision.ChangeSummary,
                revision.ValidationComment,
                revision.EffectiveFrom,
                revision.EffectiveTo,
                revision.ContentHash,
                ToRowVersion(revision.RowVersion),
                BuildNodeTree(revision.Nodes, numbers));

        public static IReadOnlyList<LegalDocumentNodeDto> BuildNodeTree(IEnumerable<LegalDocumentNode> nodes, IReadOnlyDictionary<int, string>? numbers = null)
        {
            var nodeList = nodes.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToList();
            var byParent = nodeList.GroupBy(x => x.ParentNodeId ?? 0).ToDictionary(x => x.Key, x => x.ToList());

            IReadOnlyList<LegalDocumentNodeDto> Build(int? parentId)
            {
                if (!byParent.TryGetValue(parentId ?? 0, out var children))
                {
                    return [];
                }

                return children
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Id)
                    .Select(x => ToNodeDto(x, Build(x.Id), numbers))
                    .ToList();
            }

            return Build(null);
        }

        public static LegalDocumentNodeDto ToNodeDto(
            LegalDocumentNode node,
            IReadOnlyList<LegalDocumentNodeDto> children,
            IReadOnlyDictionary<int, string>? numbers = null) =>
            new(
                node.Id,
                node.LegalDocumentRevisionId,
                node.ParentNodeId,
                node.StableKey,
                node.Type,
                node.BusinessCode,
                node.Title,
                node.EditorJson,
                node.ContentHtml,
                node.PlainText,
                node.SortOrder,
                node.IncludeInTableOfContents,
                node.StartOnNewPage,
                node.KeepWithNext,
                node.NumberingStyle,
                node.IsConditional,
                node.DisplayConditionJson,
                numbers is not null && numbers.TryGetValue(node.Id, out var number) ? number : null,
                children,
                ToRowVersion(node.RowVersion));
    }
}
