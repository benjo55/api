using System.Globalization;
using System.Text.Json;
using api.Data;
using api.Dtos.Subscription;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public sealed class SubscriptionDraftService : ISubscriptionDraftService
    {
        private const string RulesVersion = "subscription-rules-v1";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly ApplicationDBContext _db;
        private readonly IIbanValidator _ibanValidator;

        public SubscriptionDraftService(ApplicationDBContext db, IIbanValidator ibanValidator)
        {
            _db = db;
            _ibanValidator = ibanValidator;
        }

        public async Task<SubscriptionDraftDto?> GetCurrentAsync(int userId, CancellationToken cancellationToken)
        {
            var draft = await BaseQuery()
                .Where(d => d.UserId == userId && d.Status != SubscriptionDraftStatus.Cancelled && d.Status != SubscriptionDraftStatus.Active)
                .OrderByDescending(d => d.UpdatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return draft == null ? null : ToDto(draft);
        }

        public async Task<SubscriptionDraftDto> CreateAsync(int userId, CancellationToken cancellationToken)
        {
            var draft = new SubscriptionDraft
            {
                UserId = userId,
                CurrentStep = SubscriptionStepKeys.Project,
                HighestCompletedStep = 0,
                Status = SubscriptionDraftStatus.InProgress,
                StepStatusesJson = Serialize(DefaultStepStatuses()),
            };

            _db.SubscriptionDrafts.Add(draft);
            await _db.SaveChangesAsync(cancellationToken);
            await AuditAsync(draft, userId, "DraftCreated", null, null, cancellationToken);

            return ToDto(await BaseQuery().FirstAsync(d => d.Id == draft.Id, cancellationToken));
        }

        public async Task<SubscriptionDraftDto?> GetByIdAsync(int userId, int draftId, CancellationToken cancellationToken)
        {
            var draft = await FindOwnedDraftAsync(userId, draftId, cancellationToken);
            return draft == null ? null : ToDto(draft);
        }

        public async Task<SubscriptionDraftDto> SaveStepAsync(int userId, int draftId, string stepKey, JsonElement data, CancellationToken cancellationToken)
        {
            var draft = await RequireOwnedDraftAsync(userId, draftId, cancellationToken);
            var previous = GetStepJson(draft, stepKey);
            var validation = ValidateStep(stepKey, data, draft);
            if (!validation.IsValid)
            {
                await AuditAsync(draft, userId, "BusinessBlock", stepKey, Serialize(validation), cancellationToken);
                throw new InvalidOperationException(string.Join(" ", validation.Errors));
            }

            SetStepJson(draft, stepKey, data.GetRawText());
            ApplyProductSelection(draft, stepKey, data);
            CompleteStep(draft, stepKey);
            InvalidateFollowingStepsWhenStructuralDataChanges(draft, stepKey, previous, data.GetRawText());
            draft.CurrentStep = stepKey;
            draft.Status = SubscriptionDraftStatus.InProgress;
            draft.UpdatedAt = DateTime.UtcNow;
            draft.Version += 1;

            await _db.SaveChangesAsync(cancellationToken);
            await AuditAsync(draft, userId, "StepSaved", stepKey, data.GetRawText(), cancellationToken, previous);
            return ToDto(draft);
        }

        public async Task<SubscriptionDraftDto> ComputeInvestorProfileAsync(int userId, int draftId, CancellationToken cancellationToken)
        {
            var draft = await RequireOwnedDraftAsync(userId, draftId, cancellationToken);
            var profileData = ParseObject(draft.InvestorProfileDataJson);
            var situationData = ParseObject(draft.SituationDataJson);
            var projectData = ParseObject(draft.ProjectDataJson);

            var knowledge = ScoreFromTextArray(profileData, "knownProducts", 0, 18);
            var experience = ScoreFromText(profileData, "experienceLevel", new Dictionary<string, int>
            {
                ["none"] = 0,
                ["held"] = 2,
                ["occasional"] = 4,
                ["regular"] = 6,
            });
            var tolerance = ScoreFromText(profileData, "riskScenario", new Dictionary<string, int>
            {
                ["sell"] = 0,
                ["reduce"] = 2,
                ["wait"] = 5,
                ["investMore"] = 7,
            });
            var lossCapacity = ScoreFromText(situationData, "lossCapacity", new Dictionary<string, int>
            {
                ["none"] = 0,
                ["5"] = 1,
                ["10"] = 3,
                ["20"] = 5,
                ["more20"] = 7,
            });
            var horizon = ScoreFromText(projectData, "horizon", new Dictionary<string, int>
            {
                ["less2"] = 0,
                ["2to5"] = 2,
                ["5to8"] = 4,
                ["more8"] = 6,
                ["unknown"] = 1,
            });

            var blockingAlerts = new List<string>();
            if (lossCapacity == 0 && tolerance >= 5)
            {
                blockingAlerts.Add("Vous indiquez ne pouvoir supporter aucune perte tout en acceptant un scénario risqué.");
            }
            if (horizon == 0 && tolerance >= 5)
            {
                blockingAlerts.Add("Un horizon inférieur à deux ans est incompatible avec une exposition fortement risquée.");
            }

            var profile = ComputeProfile(knowledge, experience, tolerance, lossCapacity, horizon);
            var computed = new
            {
                questionnaireVersion = "investor-profile-v1",
                computedAt = DateTime.UtcNow,
                knowledgeScore = knowledge,
                experienceScore = experience,
                riskToleranceScore = tolerance,
                lossCapacityScore = lossCapacity,
                horizonScore = horizon,
                profile,
                blockingAlerts,
                warnings = blockingAlerts.Count == 0 ? Array.Empty<string>() : blockingAlerts.ToArray(),
                answers = profileData,
            };

            var previous = draft.InvestorProfileDataJson;
            draft.InvestorProfileDataJson = Serialize(computed);
            draft.UpdatedAt = DateTime.UtcNow;
            draft.Version += 1;
            CompleteStep(draft, SubscriptionStepKeys.Profile);
            MarkInvalidated(draft, SubscriptionStepKeys.Solution, SubscriptionStepKeys.Investment, SubscriptionStepKeys.Signature);

            await _db.SaveChangesAsync(cancellationToken);
            await AuditAsync(draft, userId, "InvestorProfileComputed", SubscriptionStepKeys.Profile, draft.InvestorProfileDataJson, cancellationToken, previous);
            return ToDto(draft);
        }

        public async Task<SubscriptionDraftDto> GenerateRecommendationAsync(int userId, int draftId, CancellationToken cancellationToken)
        {
            var draft = await RequireOwnedDraftAsync(userId, draftId, cancellationToken);
            var projectData = ParseObject(draft.ProjectDataJson);
            var profileData = ParseObject(draft.InvestorProfileDataJson);
            var investmentData = ParseObject(draft.InvestmentDataJson);

            var selectedProduct = draft.ProductId.HasValue
                ? await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == draft.ProductId.Value, cancellationToken)
                : null;

            var profile = ReadString(profileData, "profile") ?? "Prudent";
            var horizon = ReadString(projectData, "horizon") ?? "unknown";
            var managementMode = ReadString(investmentData, "managementMode")
                ?? ReadString(profileData, "managementPreference")
                ?? "Accompagnement par un conseiller";
            var allocation = BuildAllocation(profile);
            var warnings = new List<string>();
            if (selectedProduct == null)
            {
                warnings.Add("Aucun produit réel n'est encore retenu dans le brouillon.");
            }
            if (profile is "Sécuritaire" && allocation.Any(a => a.RiskLevel == "Dynamique"))
            {
                warnings.Add("L'allocation doit rester cohérente avec un profil sécuritaire.");
            }

            var recommendation = new SubscriptionRecommendationDto(
                Guid.NewGuid().ToString("N"),
                draft.Id,
                draft.ProductType ?? selectedProduct?.ContractFamily,
                draft.ProductId,
                managementMode,
                profile,
                LabelHorizon(horizon),
                allocation,
                BuildReasons(projectData, profile, investmentData),
                warnings,
                DateTime.UtcNow,
                RulesVersion,
                null,
                null,
                null);

            var previous = draft.RecommendationDataJson;
            draft.RecommendationDataJson = Serialize(recommendation);
            draft.UpdatedAt = DateTime.UtcNow;
            draft.Version += 1;
            CompleteStep(draft, SubscriptionStepKeys.Solution);
            MarkInvalidated(draft, SubscriptionStepKeys.Signature);

            await _db.SaveChangesAsync(cancellationToken);
            await AuditAsync(draft, userId, "RecommendationGenerated", SubscriptionStepKeys.Solution, draft.RecommendationDataJson, cancellationToken, previous);
            return ToDto(draft);
        }

        public Task<SubscriptionDraftDto> AcceptRecommendationAsync(int userId, int draftId, CancellationToken cancellationToken) =>
            UpdateRecommendationAcceptanceAsync(userId, draftId, accepted: true, reason: null, cancellationToken);

        public Task<SubscriptionDraftDto> OverrideRecommendationAsync(int userId, int draftId, string reason, CancellationToken cancellationToken) =>
            UpdateRecommendationAcceptanceAsync(userId, draftId, accepted: false, reason, cancellationToken);

        public async Task<SubscriptionDraftDto> SubmitAsync(int userId, int draftId, CancellationToken cancellationToken)
        {
            var draft = await RequireOwnedDraftAsync(userId, draftId, cancellationToken);
            var missing = RequiredSteps().Where(step => !IsCompleted(draft, step)).ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException($"Souscription incomplète. Étapes à finaliser : {string.Join(", ", missing)}.");
            }

            draft.Status = SubscriptionDraftStatus.AwaitingSignature;
            draft.CurrentStep = SubscriptionStepKeys.Signature;
            draft.SubmittedAt = DateTime.UtcNow;
            draft.UpdatedAt = DateTime.UtcNow;
            draft.Version += 1;
            await _db.SaveChangesAsync(cancellationToken);
            await AuditAsync(draft, userId, "SubmittedAwaitingSignature", SubscriptionStepKeys.Signature, null, cancellationToken);

            return ToDto(draft);
        }

        private IQueryable<SubscriptionDraft> BaseQuery() =>
            _db.SubscriptionDrafts.Include(d => d.Product);

        private async Task<SubscriptionDraft?> FindOwnedDraftAsync(int userId, int draftId, CancellationToken cancellationToken) =>
            await BaseQuery().FirstOrDefaultAsync(d => d.Id == draftId && d.UserId == userId, cancellationToken);

        private async Task<SubscriptionDraft> RequireOwnedDraftAsync(int userId, int draftId, CancellationToken cancellationToken) =>
            await FindOwnedDraftAsync(userId, draftId, cancellationToken)
            ?? throw new KeyNotFoundException("Brouillon de souscription introuvable ou non autorisé.");

        private static SubscriptionDraftDto ToDto(SubscriptionDraft draft) =>
            new(
                draft.Id,
                draft.UserId,
                draft.ProductType,
                draft.ProductId,
                draft.Product == null ? null : $"{draft.Product.ProductCode} - {draft.Product.CommercialName ?? draft.Product.ProductName}",
                draft.CurrentStep,
                draft.HighestCompletedStep,
                draft.Status,
                ParseElement(draft.ProjectDataJson),
                ParseElement(draft.SituationDataJson),
                ParseElement(draft.InvestorProfileDataJson),
                ParseElement(draft.RecommendationDataJson),
                ParseElement(draft.InvestmentDataJson),
                ParseElement(draft.ProtectionDataJson),
                ReadStepStatuses(draft.StepStatusesJson),
                draft.CreatedAt,
                draft.UpdatedAt,
                draft.SubmittedAt,
                draft.SignedAt,
                draft.Version);

        private async Task AuditAsync(SubscriptionDraft draft, int userId, string eventType, string? stepKey, string? newState, CancellationToken cancellationToken, string? previousState = null)
        {
            _db.SubscriptionDraftAuditEvents.Add(new SubscriptionDraftAuditEvent
            {
                SubscriptionDraftId = draft.Id,
                UserId = userId,
                EventType = eventType,
                StepKey = stepKey,
                PreviousStateJson = previousState,
                NewStateJson = newState,
                RulesVersion = RulesVersion,
            });
            await _db.SaveChangesAsync(cancellationToken);
        }

        private SubscriptionValidationResultDto ValidateStep(string stepKey, JsonElement data, SubscriptionDraft draft)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var values = ParseObject(data.GetRawText());

            if (stepKey == SubscriptionStepKeys.Project)
            {
                Require(values, "primaryGoal", "L'objectif principal est obligatoire.", errors);
                Require(values, "horizon", "L'horizon est obligatoire.", errors);
                Require(values, "liquidityNeed", "Le besoin de disponibilité est obligatoire.", errors);
                if (ReadString(values, "primaryGoal") == ReadString(values, "secondaryGoal"))
                {
                    errors.Add("L'objectif secondaire doit être différent de l'objectif principal.");
                }
            }
            else if (stepKey == SubscriptionStepKeys.Situation)
            {
                Require(values, "familySituation", "La situation familiale est obligatoire.", errors);
                Require(values, "professionalActivity", "L'activité professionnelle est obligatoire.", errors);
                Require(values, "taxResidence", "La résidence fiscale est obligatoire.", errors);
                Require(values, "lossCapacity", "La capacité à subir une perte est obligatoire.", errors);
                Require(values, "fundOrigins", "L'origine des fonds est obligatoire.", errors);
                if (ReadString(values, "fundOrigins")?.Contains("other", StringComparison.OrdinalIgnoreCase) == true
                    && string.IsNullOrWhiteSpace(ReadString(values, "otherFundOrigin")))
                {
                    errors.Add("Merci de préciser l'origine des fonds.");
                }
            }
            else if (stepKey == SubscriptionStepKeys.Solution)
            {
                if (!ReadInt(values, "selectedProductId").HasValue)
                {
                    errors.Add("Un produit réel doit être retenu avant de valider la proposition.");
                }
            }
            else if (stepKey == SubscriptionStepKeys.Investment)
            {
                var initial = ReadDecimal(values, "initialAmount");
                if (initial < 100)
                {
                    errors.Add("Le versement initial doit être au moins égal à 100 €.");
                }
                var scheduledEnabled = ReadBool(values, "scheduledPaymentEnabled");
                var scheduled = ReadDecimal(values, "scheduledAmount");
                if (scheduledEnabled && scheduled < 50)
                {
                    errors.Add("Le versement programmé doit être au moins égal à 50 €.");
                }
                var iban = ReadString(values, "ibanLabel");
                if (!string.IsNullOrWhiteSpace(iban) && !_ibanValidator.TryNormalizeIban(iban, out _))
                {
                    errors.Add("L'IBAN saisi n'est pas valide.");
                }
                var allocationTotal = ReadArray(values, "allocation").Sum(item => ReadDecimal(item, "percentage"));
                if (allocationTotal > 0 && allocationTotal != 100)
                {
                    errors.Add("La somme de l'allocation doit être égale à 100 %.");
                }
            }
            else if (stepKey == SubscriptionStepKeys.Protection)
            {
                var productType = draft.ProductType;
                if (productType is ContractFamily.AssuranceVie or ContractFamily.PERIndividuel)
                {
                    Require(values, "beneficiaryChoice", "Le choix de bénéficiaire est obligatoire.", errors);
                    var choice = ReadString(values, "beneficiaryChoice");
                    var customClause = ReadString(values, "customClause");
                    var beneficiaries = ReadArray(values, "beneficiaries");
                    if (choice == "custom" && string.IsNullOrWhiteSpace(customClause) && beneficiaries.Count == 0)
                    {
                        errors.Add("Renseignez une clause libre ou ajoutez au moins un bénéficiaire.");
                    }

                    if (choice == "custom")
                    {
                        var total = beneficiaries.Where(b => ReadInt(b, "rank") == 1).Sum(b => ReadDecimal(b, "percentage"));
                        if (beneficiaries.Count > 0 && total != 100)
                        {
                            errors.Add("Les bénéficiaires de rang 1 doivent totaliser 100 %.");
                        }

                        foreach (var beneficiary in beneficiaries)
                        {
                            if (string.IsNullOrWhiteSpace(ReadString(beneficiary, "firstName"))
                                || string.IsNullOrWhiteSpace(ReadString(beneficiary, "lastName"))
                                || string.IsNullOrWhiteSpace(ReadString(beneficiary, "relationship")))
                            {
                                errors.Add("Chaque bénéficiaire structuré doit comporter un prénom, un nom et un lien avec l'assuré.");
                                break;
                            }
                        }
                    }
                }
            }
            else if (stepKey == SubscriptionStepKeys.Signature)
            {
                Require(values, "documentsReceived", "La réception des documents doit être confirmée.", errors);
                Require(values, "contractTermsAccepted", "Les conditions contractuelles doivent être acceptées.", errors);
                Require(values, "informationAccuracyConfirmed", "L'exactitude des informations doit être confirmée.", errors);
                Require(values, "electronicSignatureConsent", "Le consentement à la signature électronique est obligatoire.", errors);
            }

            return new SubscriptionValidationResultDto(errors.Count == 0, errors.ToArray(), warnings.ToArray());
        }

        private async Task<SubscriptionDraftDto> UpdateRecommendationAcceptanceAsync(int userId, int draftId, bool accepted, string? reason, CancellationToken cancellationToken)
        {
            var draft = await RequireOwnedDraftAsync(userId, draftId, cancellationToken);
            if (string.IsNullOrWhiteSpace(draft.RecommendationDataJson))
            {
                throw new InvalidOperationException("Aucune recommandation n'a encore été générée.");
            }
            if (!accepted && string.IsNullOrWhiteSpace(reason))
            {
                throw new InvalidOperationException("Une justification est obligatoire pour s'écarter de la recommandation.");
            }

            var recommendation = JsonSerializer.Deserialize<SubscriptionRecommendationDto>(draft.RecommendationDataJson, JsonOptions)
                ?? throw new InvalidOperationException("Recommandation illisible.");
            var updated = recommendation with
            {
                AcceptedAt = accepted ? DateTime.UtcNow : recommendation.AcceptedAt,
                OverriddenAt = accepted ? recommendation.OverriddenAt : DateTime.UtcNow,
                OverrideReason = accepted ? recommendation.OverrideReason : reason,
            };
            var previous = draft.RecommendationDataJson;
            draft.RecommendationDataJson = Serialize(updated);
            draft.UpdatedAt = DateTime.UtcNow;
            draft.Version += 1;
            await _db.SaveChangesAsync(cancellationToken);
            await AuditAsync(draft, userId, accepted ? "RecommendationAccepted" : "RecommendationOverridden", SubscriptionStepKeys.Solution, draft.RecommendationDataJson, cancellationToken, previous);
            return ToDto(draft);
        }

        private static void ApplyProductSelection(SubscriptionDraft draft, string stepKey, JsonElement data)
        {
            if (stepKey != SubscriptionStepKeys.Solution) return;
            var values = ParseObject(data.GetRawText());
            draft.ProductId = ReadInt(values, "selectedProductId");
            var selectedFamily = ReadInt(values, "selectedContractFamily");
            draft.ProductType = selectedFamily.HasValue ? (ContractFamily)selectedFamily.Value : null;
        }

        private static void CompleteStep(SubscriptionDraft draft, string stepKey)
        {
            var statuses = ReadStepStatuses(draft.StepStatusesJson);
            statuses[stepKey] = SubscriptionStepStatus.Completed;
            var index = Array.IndexOf(SubscriptionStepKeys.Order.ToArray(), stepKey);
            if (index >= 0)
            {
                draft.HighestCompletedStep = Math.Max(draft.HighestCompletedStep, index);
            }
            draft.StepStatusesJson = Serialize(statuses);
        }

        private static void MarkInvalidated(SubscriptionDraft draft, params string[] stepKeys)
        {
            var statuses = ReadStepStatuses(draft.StepStatusesJson);
            foreach (var step in stepKeys)
            {
                if (statuses.GetValueOrDefault(step) == SubscriptionStepStatus.Completed)
                {
                    statuses[step] = SubscriptionStepStatus.Invalidated;
                }
            }
            draft.StepStatusesJson = Serialize(statuses);
        }

        private static bool IsCompleted(SubscriptionDraft draft, string stepKey) =>
            ReadStepStatuses(draft.StepStatusesJson).GetValueOrDefault(stepKey) is SubscriptionStepStatus.Completed or SubscriptionStepStatus.NotApplicable;

        private static void InvalidateFollowingStepsWhenStructuralDataChanges(SubscriptionDraft draft, string stepKey, string? previous, string next)
        {
            if (string.IsNullOrWhiteSpace(previous) || previous == next) return;
            if (stepKey == SubscriptionStepKeys.Project)
            {
                MarkInvalidated(draft, SubscriptionStepKeys.Profile, SubscriptionStepKeys.Solution, SubscriptionStepKeys.Investment, SubscriptionStepKeys.Signature);
            }
            else if (stepKey == SubscriptionStepKeys.Situation)
            {
                MarkInvalidated(draft, SubscriptionStepKeys.Profile, SubscriptionStepKeys.Solution, SubscriptionStepKeys.Investment, SubscriptionStepKeys.Signature);
            }
            else if (stepKey == SubscriptionStepKeys.Profile)
            {
                MarkInvalidated(draft, SubscriptionStepKeys.Solution, SubscriptionStepKeys.Investment, SubscriptionStepKeys.Signature);
            }
            else if (stepKey == SubscriptionStepKeys.Investment)
            {
                MarkInvalidated(draft, SubscriptionStepKeys.Solution, SubscriptionStepKeys.Signature);
            }
        }

        private static string? GetStepJson(SubscriptionDraft draft, string stepKey) => stepKey switch
        {
            SubscriptionStepKeys.Project => draft.ProjectDataJson,
            SubscriptionStepKeys.Situation => draft.SituationDataJson,
            SubscriptionStepKeys.Profile => draft.InvestorProfileDataJson,
            SubscriptionStepKeys.Solution => draft.RecommendationDataJson,
            SubscriptionStepKeys.Investment => draft.InvestmentDataJson,
            SubscriptionStepKeys.Protection => draft.ProtectionDataJson,
            SubscriptionStepKeys.Signature => null,
            _ => throw new InvalidOperationException("Étape inconnue."),
        };

        private static void SetStepJson(SubscriptionDraft draft, string stepKey, string json)
        {
            switch (stepKey)
            {
                case SubscriptionStepKeys.Project:
                    draft.ProjectDataJson = json;
                    break;
                case SubscriptionStepKeys.Situation:
                    draft.SituationDataJson = json;
                    break;
                case SubscriptionStepKeys.Profile:
                    draft.InvestorProfileDataJson = json;
                    break;
                case SubscriptionStepKeys.Solution:
                    draft.RecommendationDataJson = json;
                    break;
                case SubscriptionStepKeys.Investment:
                    draft.InvestmentDataJson = json;
                    break;
                case SubscriptionStepKeys.Protection:
                    draft.ProtectionDataJson = json;
                    break;
                case SubscriptionStepKeys.Signature:
                    break;
                default:
                    throw new InvalidOperationException("Étape inconnue.");
            }
        }

        private static Dictionary<string, SubscriptionStepStatus> DefaultStepStatuses() =>
            SubscriptionStepKeys.Order.ToDictionary(step => step, _ => SubscriptionStepStatus.NotStarted);

        private static IReadOnlyList<string> RequiredSteps() => SubscriptionStepKeys.Order;

        private static Dictionary<string, SubscriptionStepStatus> ReadStepStatuses(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return DefaultStepStatuses();
            return JsonSerializer.Deserialize<Dictionary<string, SubscriptionStepStatus>>(json, JsonOptions) ?? DefaultStepStatuses();
        }

        private static JsonElement? ParseElement(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            using var document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        }

        private static Dictionary<string, JsonElement> ParseObject(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, JsonElement>();
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone())
                : new Dictionary<string, JsonElement>();
        }

        private static IReadOnlyList<Dictionary<string, JsonElement>> ReadArray(Dictionary<string, JsonElement> values, string key)
        {
            if (!values.TryGetValue(key, out var element) || element.ValueKind != JsonValueKind.Array) return Array.Empty<Dictionary<string, JsonElement>>();
            return element.EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.Object)
                .Select(item => item.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone()))
                .ToArray();
        }

        private static string? ReadString(Dictionary<string, JsonElement> values, string key)
        {
            if (!values.TryGetValue(key, out var element)) return null;
            return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
        }

        private static decimal ReadDecimal(Dictionary<string, JsonElement> values, string key)
        {
            if (!values.TryGetValue(key, out var element)) return 0m;
            if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var value))
            {
                return value;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                var raw = element.GetString();
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariantValue))
                {
                    return invariantValue;
                }
                if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.GetCultureInfo("fr-FR"), out var frenchValue))
                {
                    return frenchValue;
                }
            }

            return 0m;
        }

        private static int? ReadInt(Dictionary<string, JsonElement> values, string key)
        {
            if (!values.TryGetValue(key, out var element)) return null;
            return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value) ? value : null;
        }

        private static bool ReadBool(Dictionary<string, JsonElement> values, string key)
        {
            if (!values.TryGetValue(key, out var element)) return false;
            return element.ValueKind == JsonValueKind.True || (element.ValueKind == JsonValueKind.String && bool.TryParse(element.GetString(), out var parsed) && parsed);
        }

        private static void Require(Dictionary<string, JsonElement> values, string key, string message, List<string> errors)
        {
            if (!values.TryGetValue(key, out var element)
                || element.ValueKind == JsonValueKind.Null
                || (element.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(element.GetString()))
                || (element.ValueKind == JsonValueKind.False))
            {
                errors.Add(message);
            }
        }

        private static int ScoreFromText(Dictionary<string, JsonElement> values, string key, Dictionary<string, int> scoreMap) =>
            ReadString(values, key) is { } raw && scoreMap.TryGetValue(raw, out var score) ? score : 0;

        private static int ScoreFromTextArray(Dictionary<string, JsonElement> values, string key, int min, int max)
        {
            if (!values.TryGetValue(key, out var element) || element.ValueKind != JsonValueKind.Array) return min;
            return Math.Clamp(element.GetArrayLength() * 2, min, max);
        }

        private static string ComputeProfile(int knowledge, int experience, int tolerance, int lossCapacity, int horizon)
        {
            if (lossCapacity <= 1 || tolerance <= 1) return "Sécuritaire";
            if (knowledge <= 3 || experience <= 1) return "Prudent";
            if (tolerance >= 6 && lossCapacity >= 5 && horizon >= 4 && knowledge >= 8) return "Offensif";
            if (tolerance >= 5 && lossCapacity >= 3 && horizon >= 4) return "Dynamique";
            return "Équilibré";
        }

        private static IReadOnlyList<SubscriptionAllocationDto> BuildAllocation(string profile) => profile switch
        {
            "Sécuritaire" => new[] { new SubscriptionAllocationDto("Fonds en euros", 100m, "Faible") },
            "Prudent" => new[] { new SubscriptionAllocationDto("Fonds en euros", 80m, "Faible"), new SubscriptionAllocationDto("Unités de compte diversifiées", 20m, "Modéré") },
            "Équilibré" => new[] { new SubscriptionAllocationDto("Fonds en euros", 55m, "Faible"), new SubscriptionAllocationDto("Unités de compte diversifiées", 45m, "Modéré") },
            "Dynamique" => new[] { new SubscriptionAllocationDto("Fonds en euros", 30m, "Faible"), new SubscriptionAllocationDto("Unités de compte diversifiées", 70m, "Dynamique") },
            _ => new[] { new SubscriptionAllocationDto("Fonds en euros", 15m, "Faible"), new SubscriptionAllocationDto("Unités de compte diversifiées", 85m, "Dynamique") },
        };

        private static IReadOnlyList<string> BuildReasons(Dictionary<string, JsonElement> project, string profile, Dictionary<string, JsonElement> investment)
        {
            var reasons = new List<string> { $"Votre profil investisseur calculé est {profile}." };
            if (ReadString(project, "horizon") == "more8") reasons.Add("Votre horizon de placement est supérieur à 8 ans.");
            if (ReadString(project, "liquidityNeed")?.Contains("partial", StringComparison.OrdinalIgnoreCase) == true) reasons.Add("Vous souhaitez conserver une partie de votre épargne disponible.");
            if (ReadDecimal(investment, "initialAmount") > 0) reasons.Add("Le versement initial permet de construire une allocation progressive.");
            return reasons;
        }

        private static string LabelHorizon(string horizon) => horizon switch
        {
            "less2" => "Moins de 2 ans",
            "2to5" => "De 2 à 5 ans",
            "5to8" => "De 5 à 8 ans",
            "more8" => "Plus de 8 ans",
            _ => "À préciser",
        };

        private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);
    }

    public static class SubscriptionStepKeys
    {
        public const string Project = "project";
        public const string Situation = "situation";
        public const string Profile = "profile";
        public const string Solution = "solution";
        public const string Investment = "investment";
        public const string Protection = "protection";
        public const string Signature = "signature";

        public static readonly IReadOnlyList<string> Order = new[]
        {
            Project,
            Situation,
            Profile,
            Solution,
            Investment,
            Protection,
            Signature,
        };
    }
}
