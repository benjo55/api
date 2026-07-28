using api.Models.Enum;

namespace api.Services.LegalDocuments
{
    internal static class LegalDocumentLexicon
    {
        public static string GetNodeTypeLabel(DocumentNodeType type) =>
            type switch
            {
                DocumentNodeType.Document => "Document",
                DocumentNodeType.Part => "Partie",
                DocumentNodeType.Title => "Titre",
                DocumentNodeType.Chapter => "Chapitre",
                DocumentNodeType.Section => "Section",
                DocumentNodeType.Article => "Article",
                DocumentNodeType.Paragraph => "Paragraphe",
                DocumentNodeType.Clause => "Clause",
                DocumentNodeType.Table => "Tableau",
                DocumentNodeType.Callout => "Encadré",
                DocumentNodeType.PageBreak => "Saut de page",
                _ => type.ToString()
            };
    }
}
