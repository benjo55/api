using System.Text.Json;
using System.Text.RegularExpressions;
using api.Data;
using api.Dtos.LegalDocuments;
using api.Interfaces;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.LegalDocuments
{
    public sealed partial class DocumentValidationService : IDocumentValidationService
    {
        private readonly ApplicationDBContext _db;
        private readonly IDocumentNumberingService _numberingService;
        private readonly IDocumentVariableResolver _variableResolver;
        private readonly IDocumentConditionEvaluator _conditionEvaluator;
        private readonly IDocumentRenderService _renderService;
        private readonly IPdfGenerationService _pdfGenerationService;

        public DocumentValidationService(
            ApplicationDBContext db,
            IDocumentNumberingService numberingService,
            IDocumentVariableResolver variableResolver,
            IDocumentConditionEvaluator conditionEvaluator,
            IDocumentRenderService renderService,
            IPdfGenerationService pdfGenerationService)
        {
            _db = db;
            _numberingService = numberingService;
            _variableResolver = variableResolver;
            _conditionEvaluator = conditionEvaluator;
            _renderService = renderService;
            _pdfGenerationService = pdfGenerationService;
        }

        public async Task<DocumentValidationResultDto> ValidateRevisionAsync(int revisionId, bool includePdfGeneration, CancellationToken cancellationToken = default)
        {
            var revision = await _db.LegalDocumentRevisions
                .AsNoTracking()
                .Include(x => x.Nodes)
                .FirstOrDefaultAsync(x => x.Id == revisionId, cancellationToken);

            if (revision is null)
            {
                throw new KeyNotFoundException("Revision not found.");
            }

            var issues = new List<DocumentValidationIssueDto>();
            var nodes = revision.Nodes.OrderBy(x => x.SortOrder).ToList();
            var roots = nodes.Where(x => x.ParentNodeId is null).ToList();
            if (roots.Count != 1 || roots[0].Type != DocumentNodeType.Document)
            {
                issues.Add(Error("TREE_ROOT", null, null, "La révision doit contenir exactement une racine de type document.", null));
            }

            foreach (var node in nodes)
            {
                if (node.ParentNodeId is not null)
                {
                    var parent = nodes.FirstOrDefault(x => x.Id == node.ParentNodeId.Value);
                    if (parent is null)
                    {
                        issues.Add(Error("BROKEN_PARENT", node.Id, node.StableKey, "L'élément parent n'existe pas.", "parentNodeId"));
                    }
                    else if (!DocumentStructureService.IsAllowedChild(parent.Type, node.Type))
                    {
                        issues.Add(Error(
                            "INVALID_NODE_RELATION",
                            node.Id,
                            node.StableKey,
                            $"{LegalDocumentLexicon.GetNodeTypeLabel(node.Type)} ne peut pas être placé sous {LegalDocumentLexicon.GetNodeTypeLabel(parent.Type)}.",
                            "type"));
                    }
                }

                if (string.IsNullOrWhiteSpace(node.Title) && node.Type is not DocumentNodeType.Paragraph and not DocumentNodeType.PageBreak)
                {
                    issues.Add(Error("TITLE_REQUIRED", node.Id, node.StableKey, "Un titre est requis pour cet élément.", "title"));
                }

                if (node.Type is DocumentNodeType.Paragraph or DocumentNodeType.Clause && string.IsNullOrWhiteSpace(node.PlainText))
                {
                    issues.Add(Error("CONTENT_REQUIRED", node.Id, node.StableKey, "Un contenu est requis pour cet élément.", "plainText"));
                }

                if (ContainsDangerousHtml(node.ContentHtml))
                {
                    issues.Add(Error("DANGEROUS_HTML", node.Id, node.StableKey, "Le contenu HTML contient un script ou des attributs d'événement interdits.", "contentHtml"));
                }

                foreach (var variable in ExtractVariables(node.ContentHtml).Concat(ExtractVariables(node.PlainText)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!_variableResolver.GetKnownVariables().Contains(variable))
                    {
                        issues.Add(Error("UNKNOWN_VARIABLE", node.Id, node.StableKey, $"La variable « {variable} » est inconnue.", "contentHtml"));
                    }
                }

                if (!_conditionEvaluator.IsValidConditionJson(node.DisplayConditionJson))
                {
                    issues.Add(Error("INVALID_CONDITION", node.Id, node.StableKey, "La condition d'affichage JSON est invalide.", "displayConditionJson"));
                }
            }

            var duplicateCodes = nodes
                .Where(x => !string.IsNullOrWhiteSpace(x.BusinessCode))
                .GroupBy(x => x.BusinessCode!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1);

            foreach (var duplicate in duplicateCodes)
            {
                foreach (var node in duplicate)
                {
                    issues.Add(Error("DUPLICATE_BUSINESS_CODE", node.Id, node.StableKey, $"Le code métier « {duplicate.Key} » est utilisé plusieurs fois.", "businessCode"));
                }
            }

            try
            {
                _ = _numberingService.GenerateNumbers(nodes);
            }
            catch (Exception ex)
            {
                issues.Add(Error("NUMBERING_FAILED", null, null, ex.Message, null));
            }

            if (includePdfGeneration && issues.All(x => x.Level != ValidationIssueLevel.Error))
            {
                try
                {
                    var model = await _renderService.BuildRenderModelAsync(revisionId, cancellationToken);
                    var html = _renderService.RenderCanonicalHtml(model);
                    _ = await _pdfGenerationService.GeneratePdfAsync(html, model.Layout.PageFormat, cancellationToken);
                }
                catch (Exception ex)
                {
                    issues.Add(Error("PDF_GENERATION_FAILED", null, null, ex.Message, null));
                }
            }

            return new DocumentValidationResultDto(issues.All(x => x.Level != ValidationIssueLevel.Error), issues);
        }

        private static DocumentValidationIssueDto Error(string code, int? nodeId, string? stableKey, string message, string? property) =>
            new(code, ValidationIssueLevel.Error, nodeId, stableKey, message, property);

        private static bool ContainsDangerousHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return false;
            }

            return DangerousHtmlRegex().IsMatch(html);
        }

        private static IEnumerable<string> ExtractVariables(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                yield break;
            }

            foreach (Match match in VariableRegex().Matches(content))
            {
                yield return match.Groups["name"].Value.Trim();
            }
        }

        [GeneratedRegex("<\\s*script|\\son[a-z]+\\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex DangerousHtmlRegex();

        [GeneratedRegex("\\{\\{\\s*(?<name>[a-zA-Z0-9_.-]+)\\s*\\}\\}", RegexOptions.Compiled)]
        private static partial Regex VariableRegex();
    }

    public sealed class DocumentVariableResolver : IDocumentVariableResolver
    {
        private static readonly IReadOnlyList<DocumentVariableDefinitionDto> Variables =
        [
            new("contract.number", "Numéro du contrat", DocumentVariableValueType.String, "Contrat", true),
            new("contract.effectiveDate", "Date d'effet du contrat", DocumentVariableValueType.Date, "Contrat", true),
            new("product.name", "Nom du produit", DocumentVariableValueType.String, "Produit", true),
            new("subscriber.fullName", "Nom complet du souscripteur", DocumentVariableValueType.String, "Personne", true),
            new("insured.fullName", "Nom complet de l'assuré", DocumentVariableValueType.String, "Personne", false),
            new("premium.amount", "Montant de la prime", DocumentVariableValueType.Currency, "Paiement", false),
            new("premium.rate", "Taux de la prime", DocumentVariableValueType.Percentage, "Paiement", false),
            new("currency", "Devise", DocumentVariableValueType.String, "Contrat", false),
            new("contract.hasScheduledPayments", "Versements programmés", DocumentVariableValueType.Boolean, "Contrat", false),
            new("subscriber.address", "Adresse du souscripteur", DocumentVariableValueType.Address, "Personne", false)
        ];

        private static readonly HashSet<string> KnownVariables = Variables
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        public IReadOnlySet<string> GetKnownVariables() => KnownVariables;

        public IReadOnlyList<DocumentVariableDefinitionDto> GetVariableDefinitions() => Variables;
    }

    public sealed class DocumentConditionEvaluator : IDocumentConditionEvaluator
    {
        public bool IsValidConditionJson(string? conditionJson)
        {
            if (string.IsNullOrWhiteSpace(conditionJson))
            {
                return true;
            }

            try
            {
                using var document = JsonDocument.Parse(conditionJson);
                return document.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
