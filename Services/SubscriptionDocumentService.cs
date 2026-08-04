using System.Net;
using System.Globalization;
using System.Text;
using System.Text.Json;
using api.Data;
using api.Dtos.LegalDocuments;
using api.Dtos.Subscription;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public sealed class SubscriptionDocumentService : ISubscriptionDocumentService
    {
        private const string CachePrefixSeed = "subscription-draft";
        private const string SummaryDocumentCode = "DOSSIER-SOUSCRIPTION";
        private const string SummaryDocumentName = "Dossier de souscription";
        private const string SummaryDocumentRole = "SubscriptionSummary";
        private readonly ApplicationDBContext _db;
        private readonly IProductDocumentAssignmentService _assignmentService;
        private readonly IDocumentRenderService _renderService;
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly IDocumentBinaryStorage _storage;
        private readonly IDocumentAuditService _auditService;

        public SubscriptionDocumentService(
            ApplicationDBContext db,
            IProductDocumentAssignmentService assignmentService,
            IDocumentRenderService renderService,
            IPdfGenerationService pdfGenerationService,
            IDocumentBinaryStorage storage,
            IDocumentAuditService auditService)
        {
            _db = db;
            _assignmentService = assignmentService;
            _renderService = renderService;
            _pdfGenerationService = pdfGenerationService;
            _storage = storage;
            _auditService = auditService;
        }

        public async Task<SubscriptionDocumentDossierDto> GetDossierAsync(
            int userId,
            int draftId,
            CancellationToken cancellationToken)
        {
            var draft = await RequireOwnedDraftAsync(userId, draftId, tracking: false, cancellationToken);
            return await BuildDossierAsync(draft, cancellationToken);
        }

        public async Task<SubscriptionDocumentDossierDto> GenerateDossierAsync(
            int userId,
            int draftId,
            string? userName,
            CancellationToken cancellationToken)
        {
            var draft = await RequireOwnedDraftAsync(userId, draftId, tracking: false, cancellationToken);
            if (!draft.ProductId.HasValue)
            {
                throw new InvalidOperationException("Aucun produit n'est retenu dans le brouillon de souscription.");
            }

            var assignments = await GetCurrentAssignmentsAsync(draft.ProductId.Value, cancellationToken);
            if (assignments.Count == 0)
            {
                await GenerateSummaryDocumentAsync(draft, userName, cancellationToken);
                return await BuildDossierAsync(draft, cancellationToken);
            }

            foreach (var assignment in assignments)
            {
                var cacheKey = BuildCacheKey(draft, assignment);
                var existing = await _db.DocumentArtifacts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.CacheKey == cacheKey
                             && x.Type == DocumentArtifactType.PreviewPdf
                             && x.LegalDocumentRevisionId == assignment.LegalDocumentRevisionId,
                        cancellationToken);

                if (existing is not null)
                {
                    try
                    {
                        await _storage.ReadAsync(existing.StorageKey, cancellationToken);
                        continue;
                    }
                    catch
                    {
                        // Le stockage local peut avoir ete purge; on regenere l'artefact du brouillon.
                    }
                }

                var renderModel = await _renderService.BuildRenderModelAsync(assignment.LegalDocumentRevisionId, cancellationToken);
                var html = AppendSubscriptionContext(_renderService.RenderCanonicalHtml(renderModel), draft, assignment);
                var pdf = await _pdfGenerationService.GeneratePdfAsync(html, renderModel.Layout.PageFormat, cancellationToken);
                var saved = await _storage.SaveAsync(pdf, ".pdf", cancellationToken);

                var artifact = new DocumentArtifact
                {
                    Type = DocumentArtifactType.PreviewPdf,
                    LegalDocumentRevisionId = assignment.LegalDocumentRevisionId,
                    StorageKey = saved.StorageKey,
                    ContentType = "application/pdf",
                    FileName = $"{Slug(renderModel.Code)}-souscription-{draft.Id}.pdf",
                    Hash = saved.Hash,
                    Size = saved.Size,
                    GeneratedBy = userName,
                    CacheKey = cacheKey,
                };

                _db.DocumentArtifacts.Add(artifact);
                await _db.SaveChangesAsync(cancellationToken);
                await _auditService.AddAsync(
                    DocumentAuditAction.PreviewGenerated,
                    null,
                    assignment.LegalDocumentRevisionId,
                    null,
                    new { subscriptionDraftId = draft.Id, artifact.Id, artifact.Hash, assignment.Role },
                    userName,
                    cancellationToken);
            }

            return await BuildDossierAsync(draft, cancellationToken);
        }

        public async Task<SubscriptionDocumentFileDto> GetDocumentFileAsync(
            int userId,
            int draftId,
            int artifactId,
            CancellationToken cancellationToken)
        {
            await RequireOwnedDraftAsync(userId, draftId, tracking: false, cancellationToken);
            var cachePrefix = BuildCachePrefix(draftId);
            var artifact = await _db.DocumentArtifacts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == artifactId
                         && x.CacheKey != null
                         && x.CacheKey.StartsWith(cachePrefix),
                    cancellationToken)
                ?? throw new KeyNotFoundException("Document de souscription introuvable.");

            var content = await _storage.ReadAsync(artifact.StorageKey, cancellationToken);
            return new SubscriptionDocumentFileDto(artifact.FileName, artifact.ContentType, content);
        }

        private async Task<SubscriptionDocumentDossierDto> BuildDossierAsync(
            SubscriptionDraft draft,
            CancellationToken cancellationToken)
        {
            if (!draft.ProductId.HasValue)
            {
                return new SubscriptionDocumentDossierDto(
                    draft.Id,
                    null,
                    null,
                    Array.Empty<SubscriptionDocumentDto>(),
                    false,
                    new[] { "Sélectionnez un produit avant de générer le dossier documentaire." });
            }

            var assignments = await GetCurrentAssignmentsAsync(draft.ProductId.Value, cancellationToken);
            var revisionIds = assignments.Select(x => x.LegalDocumentRevisionId).Distinct().ToArray();
            var cachePrefix = BuildCachePrefix(draft.Id);
            var draftArtifactRows = await _db.DocumentArtifacts
                .AsNoTracking()
                .Where(x => x.LegalDocumentRevisionId.HasValue
                            && x.CacheKey != null
                            && x.CacheKey.StartsWith(cachePrefix)
                            && x.Type == DocumentArtifactType.PreviewPdf)
                .ToListAsync(cancellationToken);
            var draftArtifacts = draftArtifactRows
                .Where(x => revisionIds.Contains(x.LegalDocumentRevisionId!.Value))
                .GroupBy(x => x.LegalDocumentRevisionId!.Value)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(a => a.GeneratedAt).First());

            var documents = assignments
                .Select(assignment =>
                {
                    draftArtifacts.TryGetValue(assignment.LegalDocumentRevisionId, out var artifact);
                    return new SubscriptionDocumentDto(
                        assignment.LegalDocumentRevisionId,
                        assignment.DocumentCode,
                        assignment.DocumentName,
                        $"{assignment.MajorVersion}.{assignment.MinorVersion}",
                        assignment.Role.ToString(),
                        LabelRole(assignment.Role),
                        artifact is null ? "À générer" : "Généré",
                        artifact?.Id,
                        artifact?.FileName,
                        artifact?.GeneratedAt,
                        artifact is null ? null : $"/api/subscriptions/drafts/{draft.Id}/documents/{artifact.Id}/download");
                })
                .ToList();

            if (assignments.Count == 0)
            {
                var summaryArtifact = await GetSummaryArtifactAsync(draft.Id, draft.Version, cancellationToken);
                documents.Add(new SubscriptionDocumentDto(
                    null,
                    SummaryDocumentCode,
                    SummaryDocumentName,
                    draft.Version.ToString(),
                    SummaryDocumentRole,
                    "Synthèse de souscription",
                    summaryArtifact is null ? "À générer" : "Généré",
                    summaryArtifact?.Id,
                    summaryArtifact?.FileName,
                    summaryArtifact?.GeneratedAt,
                    summaryArtifact is null ? null : $"/api/subscriptions/drafts/{draft.Id}/documents/{summaryArtifact.Id}/download"));
            }

            var warnings = new List<string>();
            if (assignments.Count == 0)
            {
                warnings.Add("Aucun document réglementaire n'est assigné au produit retenu. Une synthèse de souscription est générée pour permettre la préparation du dossier.");
            }
            else if (documents.Any(x => x.ArtifactId == null))
            {
                warnings.Add("Générez le dossier pour mettre à disposition les PDF de souscription.");
            }

            return new SubscriptionDocumentDossierDto(
                draft.Id,
                draft.ProductId,
                ProductLabel(draft),
                documents,
                documents.Count > 0 && documents.All(x => x.ArtifactId.HasValue),
                warnings);
        }

        private async Task<IReadOnlyList<ProductDocumentAssignmentDto>> GetCurrentAssignmentsAsync(
            int productId,
            CancellationToken cancellationToken)
        {
            var today = DateTime.UtcNow.Date;
            var assignments = await _assignmentService.GetProductAssignmentsAsync(productId, cancellationToken);
            return assignments
                .Where(x => x.IsCurrent && x.ValidFrom.Date <= today && (!x.ValidTo.HasValue || x.ValidTo.Value.Date >= today))
                .OrderBy(x => x.Role)
                .ThenBy(x => x.DocumentName)
                .ToArray();
        }

        private async Task GenerateSummaryDocumentAsync(
            SubscriptionDraft draft,
            string? userName,
            CancellationToken cancellationToken)
        {
            var cacheKey = BuildSummaryCacheKey(draft.Id, draft.Version);
            var existing = await GetSummaryArtifactAsync(draft.Id, draft.Version, cancellationToken);
            if (existing is not null)
            {
                try
                {
                    await _storage.ReadAsync(existing.StorageKey, cancellationToken);
                    return;
                }
                catch
                {
                    // Le stockage local peut avoir ete purge; on regenere l'artefact du brouillon.
                }
            }

            var html = BuildSummaryHtml(draft);
            var pdf = await _pdfGenerationService.GeneratePdfAsync(html, "A4", cancellationToken);
            var saved = await _storage.SaveAsync(pdf, ".pdf", cancellationToken);
            var artifact = new DocumentArtifact
            {
                Type = DocumentArtifactType.PreviewPdf,
                LegalDocumentRevisionId = null,
                StorageKey = saved.StorageKey,
                ContentType = "application/pdf",
                FileName = $"dossier-souscription-{draft.Id}.pdf",
                Hash = saved.Hash,
                Size = saved.Size,
                GeneratedBy = userName,
                CacheKey = cacheKey,
            };

            _db.DocumentArtifacts.Add(artifact);
            await _db.SaveChangesAsync(cancellationToken);
        }

        private async Task<DocumentArtifact?> GetSummaryArtifactAsync(
            int draftId,
            int draftVersion,
            CancellationToken cancellationToken)
        {
            var cacheKey = BuildSummaryCacheKey(draftId, draftVersion);
            return await _db.DocumentArtifacts
                .AsNoTracking()
                .Where(x => x.CacheKey == cacheKey
                            && x.Type == DocumentArtifactType.PreviewPdf
                            && x.LegalDocumentRevisionId == null)
                .OrderByDescending(x => x.GeneratedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<SubscriptionDraft> RequireOwnedDraftAsync(
            int userId,
            int draftId,
            bool tracking,
            CancellationToken cancellationToken)
        {
            var query = _db.SubscriptionDrafts.Include(x => x.Product).Where(x => x.Id == draftId && x.UserId == userId);
            if (!tracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync(cancellationToken)
                ?? throw new KeyNotFoundException("Brouillon de souscription introuvable ou non autorisé.");
        }

        private static string BuildCachePrefix(int draftId) => $"{CachePrefixSeed}:{draftId}:";

        private static string BuildCacheKey(SubscriptionDraft draft, ProductDocumentAssignmentDto assignment) =>
            $"{BuildCachePrefix(draft.Id)}revision:{assignment.LegalDocumentRevisionId}:role:{assignment.Role}:version:{draft.Version}";

        private static string BuildSummaryCacheKey(int draftId, int draftVersion) =>
            $"{BuildCachePrefix(draftId)}summary:version:{draftVersion}";

        private static string ProductLabel(SubscriptionDraft draft) =>
            draft.Product == null
                ? "Produit retenu"
                : string.Join(" - ", new[] { draft.Product.ProductCode, draft.Product.CommercialName ?? draft.Product.ProductName }.Where(x => !string.IsNullOrWhiteSpace(x)));

        private static string BuildSummaryHtml(SubscriptionDraft draft)
        {
            var project = ReadJsonObject(draft.ProjectDataJson);
            var situation = ReadJsonObject(draft.SituationDataJson);
            var profile = ReadJsonObject(draft.InvestorProfileDataJson);
            var recommendation = ReadJsonObject(draft.RecommendationDataJson);
            var investment = ReadJsonObject(draft.InvestmentDataJson);
            var protection = ReadJsonObject(draft.ProtectionDataJson);

            var html = new StringBuilder();
            html.Append("""
                <!doctype html>
                <html lang="fr">
                <head>
                  <meta charset="utf-8">
                  <style>
                    body { font-family: Arial, Helvetica, sans-serif; color: #102033; margin: 32px; line-height: 1.45; }
                    h1 { font-size: 28px; margin: 0 0 8px; color: #032b55; }
                    h2 { font-size: 18px; margin: 24px 0 8px; color: #06447a; border-bottom: 1px solid #cbd8e6; padding-bottom: 4px; }
                    .subtitle { color: #54687d; margin-bottom: 20px; }
                    table { width: 100%; border-collapse: collapse; margin-top: 8px; }
                    th, td { border: 1px solid #d0d7de; padding: 7px 9px; vertical-align: top; }
                    th { width: 34%; text-align: left; background: #f4f7fb; }
                    .warning { background: #fff4d6; border: 1px solid #f2d486; padding: 12px; margin: 18px 0; }
                  </style>
                </head>
                <body>
                """);
            html.Append("<h1>Dossier de souscription</h1>");
            html.Append("<p class=\"subtitle\">Synthèse générée depuis le parcours de souscription Financial Life.</p>");
            html.Append("<div class=\"warning\">Aucun document réglementaire n'est assigné au produit retenu dans la bibliothèque documentaire. Cette synthèse permet de préparer le dossier, mais l'affectation documentaire produit reste à compléter par le back-office.</div>");

            html.Append("<h2>Contrat demandé</h2><table>");
            AddRow(html, "Brouillon", draft.Id.ToString(CultureInfo.InvariantCulture));
            AddRow(html, "Produit", ProductLabel(draft));
            AddRow(html, "Famille", draft.ProductType?.ToString());
            AddRow(html, "Date de préparation", DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("fr-FR")));
            html.Append("</table>");

            html.Append("<h2>Projet</h2><table>");
            AddJsonRow(html, project, "primaryGoal", "Objectif principal");
            AddJsonRow(html, project, "secondaryGoal", "Objectif secondaire");
            AddJsonRow(html, project, "horizon", "Horizon");
            AddJsonRow(html, project, "liquidityNeed", "Disponibilité des fonds");
            AddJsonRow(html, project, "targetAmount", "Montant cible");
            html.Append("</table>");

            html.Append("<h2>Situation</h2><table>");
            AddJsonRow(html, situation, "familySituation", "Situation familiale");
            AddJsonRow(html, situation, "dependants", "Personnes à charge");
            AddJsonRow(html, situation, "birthDate", "Date de naissance");
            AddJsonRow(html, situation, "professionalActivity", "Activité professionnelle");
            AddJsonRow(html, situation, "residenceCountry", "Pays de résidence");
            AddJsonRow(html, situation, "taxResidence", "Résidence fiscale");
            AddJsonRow(html, situation, "annualIncomeRange", "Revenus nets annuels");
            AddJsonRow(html, situation, "monthlySavingsCapacity", "Capacité d'épargne mensuelle");
            AddJsonRow(html, situation, "totalWealthRange", "Patrimoine approximatif");
            html.Append("</table>");

            html.Append("<h2>Profil et recommandation</h2><table>");
            AddJsonRow(html, profile, "riskLevel", "Profil de risque");
            AddJsonRow(html, profile, "managementPreference", "Préférence de gestion");
            AddJsonRow(html, recommendation, "managementMode", "Mode de gestion recommandé");
            AddJsonRow(html, recommendation, "recommendedHorizon", "Horizon recommandé");
            html.Append("</table>");

            html.Append("<h2>Investissement</h2><table>");
            AddJsonRow(html, investment, "initialAmount", "Versement initial");
            AddJsonRow(html, investment, "paymentMode", "Mode de paiement");
            AddJsonRow(html, investment, "firstPaymentDate", "Date souhaitée du premier versement");
            AddJsonRow(html, investment, "scheduledPaymentEnabled", "Versement programmé");
            AddJsonRow(html, investment, "scheduledAmount", "Montant périodique");
            AddJsonRow(html, investment, "scheduledFrequency", "Périodicité");
            AddJsonRow(html, investment, "managementMode", "Mode de gestion");
            html.Append("</table>");
            html.Append(BuildAllocationHtml(investment));

            html.Append("<h2>Protection et bénéficiaires</h2><table>");
            AddJsonRow(html, protection, "beneficiaryChoice", "Type de clause");
            AddJsonRow(html, protection, "customClause", "Clause personnalisée");
            html.Append("</table>");
            html.Append("</body></html>");
            return html.ToString();
        }

        private static void AddJsonRow(StringBuilder builder, Dictionary<string, JsonElement> values, string key, string label)
        {
            var value = ReadJsonValue(values, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                AddRow(builder, label, value);
            }
        }

        private static string BuildAllocationHtml(Dictionary<string, JsonElement> investment)
        {
            if (!investment.TryGetValue("allocation", out var allocation)
                || allocation.ValueKind != JsonValueKind.Array)
            {
                return string.Empty;
            }

            var rows = new StringBuilder();
            rows.Append("<h2>Allocation initiale</h2><table><tr><th>Poche</th><th>Risque</th><th>Pourcentage</th></tr>");
            foreach (var row in allocation.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object) continue;
                rows.Append("<tr><td>");
                rows.Append(WebUtility.HtmlEncode(ReadJsonObjectValue(row, "label")));
                rows.Append("</td><td>");
                rows.Append(WebUtility.HtmlEncode(ReadJsonObjectValue(row, "riskLevel")));
                rows.Append("</td><td>");
                rows.Append(WebUtility.HtmlEncode(ReadJsonObjectValue(row, "percentage")));
                rows.Append(" %</td></tr>");
            }

            rows.Append("</table>");
            return rows.ToString();
        }

        private static string LabelRole(ProductDocumentRole role) => role switch
        {
            ProductDocumentRole.GeneralTerms => "Conditions générales",
            ProductDocumentRole.Notice => "Notice",
            ProductDocumentRole.RegulatoryNotice => "Document réglementaire",
            _ => role.ToString(),
        };

        private static string AppendSubscriptionContext(
            string html,
            SubscriptionDraft draft,
            ProductDocumentAssignmentDto assignment)
        {
            var appendix = new StringBuilder();
            appendix.Append("<section class=\"subscription-context\" style=\"page-break-before: always; font-family: Arial, Helvetica, sans-serif;\">");
            appendix.Append("<h1>Dossier de souscription</h1>");
            appendix.Append("<p>Ce document est rattache au brouillon de souscription ");
            appendix.Append(WebUtility.HtmlEncode(draft.Id.ToString()));
            appendix.Append(" pour le produit ");
            appendix.Append(WebUtility.HtmlEncode(ProductLabel(draft)));
            appendix.Append(".</p><table style=\"width:100%; border-collapse:collapse;\">");
            AddRow(appendix, "Document", $"{assignment.DocumentCode} - {assignment.DocumentName}");
            AddRow(appendix, "Version", $"{assignment.MajorVersion}.{assignment.MinorVersion}");
            AddRow(appendix, "Role", LabelRole(assignment.Role));
            AddRow(appendix, "Date de preparation", DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"));
            AddRow(appendix, "Versement initial", ReadJsonValue(draft.InvestmentDataJson, "initialAmount"));
            AddRow(appendix, "Mode de gestion", ReadJsonValue(draft.InvestmentDataJson, "managementMode"));
            AddRow(appendix, "Clause beneficiaire", ReadJsonValue(draft.ProtectionDataJson, "beneficiaryChoice"));
            appendix.Append("</table></section>");

            var closingBodyIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
            if (closingBodyIndex < 0)
            {
                return html + appendix;
            }

            return html.Insert(closingBodyIndex, appendix.ToString());
        }

        private static void AddRow(StringBuilder builder, string label, string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            builder.Append("<tr><th style=\"text-align:left; border:1px solid #d0d7de; padding:6px; width:30%;\">");
            builder.Append(WebUtility.HtmlEncode(label));
            builder.Append("</th><td style=\"border:1px solid #d0d7de; padding:6px;\">");
            builder.Append(WebUtility.HtmlEncode(value));
            builder.Append("</td></tr>");
        }

        private static string? ReadJsonValue(string? json, string key)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(key, out var value)
                || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }

        private static Dictionary<string, JsonElement> ReadJsonObject(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, JsonElement>();
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.Clone())
                : new Dictionary<string, JsonElement>();
        }

        private static string? ReadJsonValue(Dictionary<string, JsonElement> values, string key)
        {
            if (!values.TryGetValue(key, out var value)) return null;
            return ReadJsonValue(value, null);
        }

        private static string? ReadJsonValue(JsonElement value, string? _)
        {
            if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
            if (value.ValueKind == JsonValueKind.String) return value.GetString();
            if (value.ValueKind == JsonValueKind.True) return "Oui";
            if (value.ValueKind == JsonValueKind.False) return "Non";
            return value.ToString();
        }

        private static string? ReadJsonObjectValue(JsonElement obj, string key)
        {
            if (obj.ValueKind != JsonValueKind.Object || !obj.TryGetProperty(key, out var value)) return null;
            return ReadJsonValue(value, null);
        }

        private static string Slug(string value)
        {
            var builder = new StringBuilder();
            foreach (var c in value.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c)) builder.Append(c);
                else if (builder.Length == 0 || builder[^1] != '-') builder.Append('-');
            }

            return builder.ToString().Trim('-');
        }
    }
}
