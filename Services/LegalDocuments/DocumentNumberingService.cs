using api.Interfaces;
using api.Models;
using api.Models.Enum;

namespace api.Services.LegalDocuments
{
    public sealed class DocumentNumberingService : IDocumentNumberingService
    {
        private static readonly HashSet<DocumentNodeType> NumberedTypes =
        [
            DocumentNodeType.Part,
            DocumentNodeType.Title,
            DocumentNodeType.Chapter,
            DocumentNodeType.Section,
            DocumentNodeType.Article,
            DocumentNodeType.Clause
        ];

        public IReadOnlyDictionary<int, string> GenerateNumbers(IEnumerable<LegalDocumentNode> nodes)
        {
            var orderedNodes = nodes.OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToList();
            var byParent = orderedNodes.GroupBy(x => x.ParentNodeId ?? 0).ToDictionary(x => x.Key, x => x.ToList());
            var numbers = new Dictionary<int, string>();

            void Walk(int? parentId, string prefix)
            {
                if (!byParent.TryGetValue(parentId ?? 0, out var children))
                {
                    return;
                }

                var counter = 0;
                foreach (var child in children.OrderBy(x => x.SortOrder).ThenBy(x => x.Id))
                {
                    var childPrefix = prefix;
                    if (NumberedTypes.Contains(child.Type))
                    {
                        if (string.Equals(child.NumberingStyle, "none", StringComparison.OrdinalIgnoreCase))
                        {
                            childPrefix = prefix;
                        }
                        else if (
                            string.Equals(child.NumberingStyle, "manual", StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(child.BusinessCode))
                        {
                            childPrefix = child.BusinessCode.Trim();
                            numbers[child.Id] = childPrefix;
                        }
                        else
                        {
                            counter++;
                            childPrefix = string.IsNullOrWhiteSpace(prefix) ? counter.ToString() : $"{prefix}.{counter}";
                            numbers[child.Id] = childPrefix;
                        }
                    }

                    Walk(child.Id, childPrefix);
                }
            }

            Walk(null, string.Empty);
            return numbers;
        }
    }
}
