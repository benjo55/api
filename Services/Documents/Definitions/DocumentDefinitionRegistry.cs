using api.Interfaces.Documents;

namespace api.Services.Documents.Definitions
{
    public sealed class DocumentDefinitionRegistry : IDocumentDefinitionRegistry
    {
        private readonly IReadOnlyDictionary<string, DocumentDefinition> _definitions;

        public DocumentDefinitionRegistry(IEnumerable<DocumentDefinition> definitions)
        {
            _definitions = definitions.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        }

        public DocumentDefinition? Find(string key) =>
            _definitions.TryGetValue(key, out var definition) ? definition : null;

        public IReadOnlyCollection<DocumentDefinition> List() => _definitions.Values.ToArray();
    }
}
