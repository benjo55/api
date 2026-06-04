using System.Globalization;
using System.Text.Json;
using api.Dtos.Pdf;
using api.Interfaces;
using api.Models;

namespace api.Services.Pdf
{
    public sealed class PdfBusinessDocumentService : IPdfBusinessDocumentService
    {
        private readonly IContractRepository _contractRepository;
        private readonly IOperationRepository _operationRepository;
        private readonly ISupportHistoricalDataRepository _supportHistoricalDataRepository;
        private readonly IPdfDocumentService _pdfDocumentService;

        public PdfBusinessDocumentService(
            IContractRepository contractRepository,
            IOperationRepository operationRepository,
            ISupportHistoricalDataRepository supportHistoricalDataRepository,
            IPdfDocumentService pdfDocumentService)
        {
            _contractRepository = contractRepository;
            _operationRepository = operationRepository;
            _supportHistoricalDataRepository = supportHistoricalDataRepository;
            _pdfDocumentService = pdfDocumentService;
        }

        public async Task<PdfGeneratedFileDto> GenerateContractSheetAsync(GenerateContractSheetRequestDto request, CancellationToken cancellationToken = default)
        {
            var contract = await LoadContractOrThrowAsync(request.ContractId);
            var operations = await LoadOperationsAsync(request.ContractId);
            var valuationCharts = await BuildSupportValuationChartsAsync(contract, cancellationToken);

            var documentRequest = BuildContractSheetDocument(contract, operations, request.FileName, request.LogoBase64, request.LogoUrl, request.QrCodeContent, valuationCharts);
            return await _pdfDocumentService.GenerateAsync(documentRequest, cancellationToken);
        }

        public async Task<PdfGeneratedFileDto> GenerateClientCaseFileAsync(GenerateClientCaseFileRequestDto request, CancellationToken cancellationToken = default)
        {
            var contract = await LoadContractOrThrowAsync(request.ContractId);
            var operations = await LoadOperationsAsync(request.ContractId);
            var valuationCharts = await BuildSupportValuationChartsAsync(contract, cancellationToken);
            var generatedParts = new List<MergePdfPartDto>();

            if (request.IncludeContractSheet)
            {
                var contractSheet = BuildContractSheetDocument(contract, operations, $"{request.FileName}-fiche-contrat", request.LogoBase64, request.LogoUrl, BuildContractQrContent(contract), valuationCharts);
                var contractFile = await _pdfDocumentService.GenerateAsync(contractSheet, cancellationToken);
                generatedParts.Add(ToMergePart(contractFile, "fiche-contrat"));
            }

            if (request.IncludeSituationStatement)
            {
                var statement = BuildSituationStatementDocument(contract, operations, $"{request.FileName}-releve-situation", request.LogoBase64, request.LogoUrl, valuationCharts);
                var statementFile = await _pdfDocumentService.GenerateAsync(statement, cancellationToken);
                generatedParts.Add(ToMergePart(statementFile, "releve-situation"));
            }

            if (request.IncludeOperationsHistory)
            {
                var history = BuildOperationsHistoryDocument(contract, operations, $"{request.FileName}-historique-operations", request.LogoBase64, request.LogoUrl);
                var historyFile = await _pdfDocumentService.GenerateAsync(history, cancellationToken);
                generatedParts.Add(ToMergePart(historyFile, "historique-operations"));
            }

            if (request.IncludeAssetAllocationReport)
            {
                var allocation = BuildAssetAllocationDocument(contract, $"{request.FileName}-allocation-actifs", request.LogoBase64, request.LogoUrl);
                var allocationFile = await _pdfDocumentService.GenerateAsync(allocation, cancellationToken);
                generatedParts.Add(ToMergePart(allocationFile, "allocation-actifs"));
            }

            if (request.AdditionalDocuments.Count > 0)
            {
                generatedParts.AddRange(request.AdditionalDocuments);
            }

            if (generatedParts.Count == 0)
            {
                throw new ArgumentException("No document selected for dossier generation.", nameof(request));
            }

            var mergeRequest = new MergePdfRequestDto
            {
                FileName = request.FileName,
                Documents = generatedParts
            };

            return await _pdfDocumentService.MergeAsync(mergeRequest, cancellationToken);
        }

        private async Task<Contract> LoadContractOrThrowAsync(int contractId)
        {
            var contract = await _contractRepository.LoadContractById(contractId);
            return contract ?? throw new KeyNotFoundException($"Contract '{contractId}' not found.");
        }

        private async Task<List<Operation>> LoadOperationsAsync(int contractId)
        {
            var operations = await _operationRepository.GetByContractAsync(contractId);
            return operations
                .OrderByDescending(x => x.OperationDate)
                .ToList();
        }

        private static GeneratePdfRequestDto BuildContractSheetDocument(
            Contract contract,
            List<Operation> operations,
            string fileName,
            string? logoBase64,
            string? logoUrl,
            string? qrContent,
            List<PdfChartDto> charts)
        {
            var personName = BuildPersonName(contract.Person);

            return new GeneratePdfRequestDto
            {
                DocumentType = PdfDocumentType.ContractSheet,
                FileName = fileName,
                Title = $"Fiche contrat - {contract.ContractNumber}",
                SubTitle = personName,
                Reference = contract.ExternalReference,
                LogoBase64 = logoBase64,
                LogoUrl = logoUrl,
                Charts = charts,
                QrCodeContent = string.IsNullOrWhiteSpace(qrContent) ? BuildContractQrContent(contract) : qrContent,
                Metadata =
                {
                    new PdfMetadataItemDto { Key = "Numéro", Value = contract.ContractNumber },
                    new PdfMetadataItemDto { Key = "Libellé", Value = contract.ContractLabel },
                    new PdfMetadataItemDto { Key = "Type", Value = contract.ContractType },
                    new PdfMetadataItemDto { Key = "Statut", Value = contract.Status },
                    new PdfMetadataItemDto { Key = "Date d'effet", Value = FormatDate(contract.DateEffect) },
                    new PdfMetadataItemDto { Key = "Date de signature", Value = FormatDate(contract.DateSign) },
                    new PdfMetadataItemDto { Key = "Produit", Value = contract.Product?.ProductName ?? "-" },
                    new PdfMetadataItemDto { Key = "Devise", Value = contract.Currency },
                    new PdfMetadataItemDto { Key = "Valeur actuelle", Value = FormatMoney(contract.CurrentValue, contract.Currency) },
                    new PdfMetadataItemDto { Key = "Prime initiale", Value = FormatMoney(contract.InitialPremium, contract.Currency) }
                },
                Sections =
                {
                    new PdfSectionDto
                    {
                        Title = "Titulaire",
                        Content = BuildHolderSectionContent(contract, personName)
                    },
                    new PdfSectionDto
                    {
                        Title = "Commentaire conseiller",
                        Content = string.IsNullOrWhiteSpace(contract.AdvisorComment) ? "Aucun commentaire" : contract.AdvisorComment
                    }
                },
                Tables =
                {
                    BuildSupportsTable(contract),
                    BuildRecentOperationsTable(operations, contract.Currency)
                }
            };
        }

        private static GeneratePdfRequestDto BuildSituationStatementDocument(
            Contract contract,
            List<Operation> operations,
            string fileName,
            string? logoBase64,
            string? logoUrl,
            List<PdfChartDto> charts)
        {
            var executed = operations.Where(x => x.Status == OperationStatus.Executed).ToList();
            var pending = operations.Where(x => x.Status == OperationStatus.Pending).ToList();

            return new GeneratePdfRequestDto
            {
                DocumentType = PdfDocumentType.SituationStatement,
                FileName = fileName,
                Title = $"Relevé de situation - {contract.ContractNumber}",
                SubTitle = BuildPersonName(contract.Person),
                LogoBase64 = logoBase64,
                LogoUrl = logoUrl,
                Charts = charts,
                Metadata =
                {
                    new PdfMetadataItemDto { Key = "Valeur actuelle", Value = FormatMoney(contract.CurrentValue, contract.Currency) },
                    new PdfMetadataItemDto { Key = "Net investi", Value = FormatMoney(contract.NetInvested, contract.Currency) },
                    new PdfMetadataItemDto { Key = "Performance", Value = FormatPercent(contract.PerformancePercent) },
                    new PdfMetadataItemDto { Key = "Versements exécutés", Value = FormatMoney(contract.PaidExecuted, contract.Currency) },
                    new PdfMetadataItemDto { Key = "Versements en attente", Value = FormatMoney(contract.PaidPending, contract.Currency) },
                    new PdfMetadataItemDto { Key = "Rachats exécutés", Value = FormatMoney(contract.WithdrawnExecuted, contract.Currency) },
                    new PdfMetadataItemDto { Key = "Rachats en attente", Value = FormatMoney(contract.WithdrawnPending, contract.Currency) },
                    new PdfMetadataItemDto { Key = "Opérations exécutées", Value = executed.Count.ToString(CultureInfo.InvariantCulture) },
                    new PdfMetadataItemDto { Key = "Opérations en attente", Value = pending.Count.ToString(CultureInfo.InvariantCulture) }
                },
                Tables =
                {
                    BuildSupportsTable(contract)
                }
            };
        }

        private static GeneratePdfRequestDto BuildOperationsHistoryDocument(
            Contract contract,
            List<Operation> operations,
            string fileName,
            string? logoBase64,
            string? logoUrl)
        {
            var table = new PdfTableDto
            {
                Title = "Historique des opérations",
                Headers = new List<string> { "Date", "Type", "Statut", "Montant", "Exécution" },
                Rows = operations
                    .Select(operation => new List<string>
                    {
                        FormatDate(operation.OperationDate),
                        TranslateOperationType(operation.Type),
                        TranslateOperationStatus(operation.Status),
                        FormatMoney(operation.Amount ?? 0m, operation.Currency),
                        operation.ExecutionDate.HasValue ? FormatDate(operation.ExecutionDate.Value) : "-"
                    })
                    .ToList()
            };

            return new GeneratePdfRequestDto
            {
                DocumentType = PdfDocumentType.OperationsHistory,
                FileName = fileName,
                Title = $"Historique des opérations - {contract.ContractNumber}",
                SubTitle = BuildPersonName(contract.Person),
                LogoBase64 = logoBase64,
                LogoUrl = logoUrl,
                Tables = { table }
            };
        }

        private static GeneratePdfRequestDto BuildAssetAllocationDocument(
            Contract contract,
            string fileName,
            string? logoBase64,
            string? logoUrl)
        {
            var compartmentById = contract.Compartments.ToDictionary(x => x.Id, x => x.Label);
            var totalCurrentAmount = contract.Supports.Sum(x => x.CurrentAmount);
            var orderedSupports = OrderSupportsForDisplay(contract.Supports, compartmentById).ToList();
            var table = new PdfTableDto
            {
                Title = "Allocation des actifs",
                Headers = new List<string> { "Support", "Poche", "Montant", "% allocation", "Performance" },
                Rows = orderedSupports
                    .Select(support =>
                    {
                        var amount = support.CurrentAmount;
                        var weight = totalCurrentAmount <= 0m ? 0m : amount / totalCurrentAmount;
                        return new List<string>
                        {
                            support.Support?.Label ?? $"Support #{support.SupportId}",
                            ResolveCompartmentLabel(compartmentById, support.CompartmentId),
                            FormatMoney(amount, contract.Currency),
                            $"{weight:P2}",
                            $"{support.Performance:0.##}%"
                        };
                    })
                    .ToList()
            };

            return new GeneratePdfRequestDto
            {
                DocumentType = PdfDocumentType.AssetAllocationReport,
                FileName = fileName,
                Title = $"Rapport d'allocation d'actifs - {contract.ContractNumber}",
                SubTitle = BuildPersonName(contract.Person),
                LogoBase64 = logoBase64,
                LogoUrl = logoUrl,
                Tables = { table }
            };
        }

        private static PdfTableDto BuildSupportsTable(Contract contract)
        {
            var compartmentById = contract.Compartments.ToDictionary(x => x.Id, x => x.Label);
            var orderedSupports = OrderSupportsForDisplay(contract.Supports, compartmentById);
            return new PdfTableDto
            {
                Title = "Synthèse des supports",
                Headers = new List<string> { "Support", "Poche", "Investi", "Valorisation", "Performance" },
                Rows = orderedSupports
                    .Select(support => new List<string>
                    {
                        support.Support?.Label ?? $"Support #{support.SupportId}",
                        ResolveCompartmentLabel(compartmentById, support.CompartmentId),
                        FormatMoney(support.InvestedAmount, contract.Currency),
                        FormatMoney(support.CurrentAmount, contract.Currency),
                        $"{support.Performance:0.##}%"
                    })
                    .ToList()
            };
        }

        private static PdfTableDto BuildRecentOperationsTable(List<Operation> operations, string currency)
        {
            return new PdfTableDto
            {
                Title = "Dernières opérations",
                Headers = new List<string> { "Date", "Type", "Statut", "Montant" },
                Rows = operations
                    .Take(10)
                    .Select(operation => new List<string>
                    {
                        FormatDate(operation.OperationDate),
                        TranslateOperationType(operation.Type),
                        TranslateOperationStatus(operation.Status),
                        FormatMoney(operation.Amount ?? 0m, string.IsNullOrWhiteSpace(operation.Currency) ? currency : operation.Currency)
                    })
                    .ToList()
            };
        }

        private static MergePdfPartDto ToMergePart(PdfGeneratedFileDto file, string fallbackName)
        {
            return new MergePdfPartDto
            {
                FileName = string.IsNullOrWhiteSpace(file.FileName) ? fallbackName : file.FileName,
                Base64Content = Convert.ToBase64String(file.Content)
            };
        }

        private static string BuildPersonName(Person? person)
        {
            if (person is null)
            {
                return "Titulaire non renseigné";
            }

            var firstName = string.IsNullOrWhiteSpace(person.FirstName) ? string.Empty : person.FirstName.Trim();
            var lastName = string.IsNullOrWhiteSpace(person.LastName) ? string.Empty : person.LastName.Trim();
            var fullName = $"{firstName} {lastName}".Trim();

            return string.IsNullOrWhiteSpace(fullName) ? "Titulaire non renseigné" : fullName;
        }

        private static string BuildContractQrContent(Contract contract)
        {
            return $"Contrat:{contract.ContractNumber}|Titulaire:{BuildPersonName(contract.Person)}|Valeur:{contract.CurrentValue}";
        }

        private static string BuildHolderSectionContent(Contract contract, string personName)
        {
            var lines = new List<string>
            {
                personName
            };

            lines.Add($"Email : {FormatNullable(contract.Person?.Email1)}");
            lines.Add($"Mobile : {FormatNullable(contract.Person?.PhoneNumber)}");

            var sourceAddress = !string.IsNullOrWhiteSpace(contract.PostalAddress)
                ? contract.PostalAddress
                : contract.Person?.PostalAddress;
            lines.Add(BuildDetailedAddress(sourceAddress));

            return string.Join("\n", lines);
        }

        private static string BuildDetailedAddress(string? rawAddress)
        {
            if (string.IsNullOrWhiteSpace(rawAddress))
            {
                return "Adresse : -";
            }

            var trimmed = rawAddress.Trim();
            if (!trimmed.StartsWith("{"))
            {
                return $"Adresse : {trimmed}";
            }

            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var root = doc.RootElement;

                var label = ReadAddressField(root, "label");
                var street = ReadAddressField(root, "street")
                    ?? ReadAddressField(root, "rue")
                    ?? ReadAddressField(root, "line1");
                var city = ReadAddressField(root, "city") ?? ReadAddressField(root, "ville");
                var postalCode = ReadAddressField(root, "postalCode")
                    ?? ReadAddressField(root, "codePostal")
                    ?? ReadAddressField(root, "postcode");

                if (!string.IsNullOrWhiteSpace(label))
                {
                    return $"Adresse : {label}";
                }

                var cityLine = string.Join(" ", new[] { postalCode, city }.Where(x => !string.IsNullOrWhiteSpace(x)));
                var fullAddress = string.Join(", ", new[] { street, cityLine }.Where(x => !string.IsNullOrWhiteSpace(x)));

                return string.IsNullOrWhiteSpace(fullAddress)
                    ? $"Adresse : {trimmed}"
                    : $"Adresse : {fullAddress}";
            }
            catch
            {
                return $"Adresse : {trimmed}";
            }
        }

        private static string? ReadAddressField(JsonElement root, string fieldName)
        {
            if (TryReadString(root, fieldName, out var direct))
            {
                return direct;
            }

            if (root.TryGetProperty("properties", out var properties) && TryReadString(properties, fieldName, out var fromProperties))
            {
                return fromProperties;
            }

            return null;
        }

        private static bool TryReadString(JsonElement root, string fieldName, out string? value)
        {
            value = null;

            if (!root.TryGetProperty(fieldName, out var element))
            {
                return false;
            }

            value = element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }

        private static string FormatNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
        }

        private static IEnumerable<FinancialSupportAllocation> OrderSupportsForDisplay(
            IEnumerable<FinancialSupportAllocation> supports,
            IReadOnlyDictionary<int, string> compartmentById)
        {
            return supports
                .OrderByDescending(support =>
                {
                    var label = ResolveCompartmentLabel(compartmentById, support.CompartmentId);
                    return string.Equals(label, "Global", StringComparison.OrdinalIgnoreCase);
                })
                .ThenByDescending(support => support.CurrentAmount)
                .ThenBy(support => support.Support?.Label ?? string.Empty);
        }

        private async Task<List<PdfChartDto>> BuildSupportValuationChartsAsync(Contract contract, CancellationToken cancellationToken)
        {
            const int maxPoints = 52;
            var oneYearAgo = DateTime.UtcNow.Date.AddYears(-1);

            var supports = contract.Supports
                .GroupBy(x => x.SupportId)
                .Select(group => group
                    .OrderByDescending(x => x.CurrentAmount)
                    .ThenByDescending(x => x.InvestedAmount)
                    .First())
                .ToList();

            if (supports.Count == 0)
            {
                return new List<PdfChartDto>();
            }

            var charts = new List<PdfChartDto>();

            foreach (var supportAllocation in supports)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var supportId = supportAllocation.SupportId;
                var history = await _supportHistoricalDataRepository.GetBySupportIdAsync(supportId);
                var points = history
                    .OrderBy(h => h.Date)
                    .Select(h => new
                    {
                        Date = h.Date.Date,
                        Value = h.Nav ?? h.Close
                    })
                    .Where(x => x.Value.HasValue && x.Date >= oneYearAgo)
                    .Select(x => new ValuationPoint(x.Date, x.Value!.Value))
                    .ToList();

                if (points.Count < 2)
                {
                    points = history
                        .OrderBy(h => h.Date)
                        .Select(h => new
                        {
                            Date = h.Date.Date,
                            Value = h.Nav ?? h.Close
                        })
                        .Where(x => x.Value.HasValue)
                        .Select(x => new ValuationPoint(x.Date, x.Value!.Value))
                        .TakeLast(maxPoints)
                        .ToList();
                }

                if (points.Count < 2)
                {
                    points = BuildFallbackPoints(supportAllocation);
                }

                var sampled = Downsample(points, maxPoints);
                if (sampled.Count < 2)
                {
                    continue;
                }

                var supportLabel = supportAllocation.Support?.Label;

                var labels = sampled
                    .Select(x => x.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))
                    .ToList();
                var values = sampled
                    .Select(x => Math.Round((double)x.Value, 4))
                    .ToList();
                var chartTitle = string.IsNullOrWhiteSpace(supportLabel) ? $"Support {supportId}" : supportLabel;
                var perfOneYear = ComputePerformance(sampled.First().Value, sampled.Last().Value);
                var perfText = FormatSignedPercent(perfOneYear);

                var config = new
                {
                    type = "line",
                    data = new
                    {
                        labels,
                        datasets = new[]
                        {
                            new
                            {
                                label = chartTitle,
                                data = values,
                                fill = false,
                                tension = 0.25,
                                borderWidth = 2,
                                pointRadius = 0
                            }
                        }
                    },
                    options = new
                    {
                        locale = "fr-FR",
                        plugins = new
                        {
                            title = new { display = false },
                            legend = new { display = false },
                            tooltip = new
                            {
                                callbacks = new
                                {
                                    label = "function(context){ return new Intl.NumberFormat('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(context.parsed.y) + ' €'; }"
                                }
                            }
                        },
                        elements = new
                        {
                            line = new { borderJoinStyle = "round" }
                        },
                        scales = new
                        {
                            x = new
                            {
                                display = true,
                                ticks = new
                                {
                                    autoSkip = true,
                                    maxTicksLimit = 8,
                                    maxRotation = 45,
                                    minRotation = 45,
                                },
                                grid = new { color = "rgba(180, 190, 210, 0.20)" }
                            },
                            y = new
                            {
                                display = true,
                                ticks = new
                                {
                                    callback = "function(value){ return new Intl.NumberFormat('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(value) + ' €'; }"
                                },
                                grid = new { color = "rgba(180, 190, 210, 0.20)" }
                            }
                        }
                    }
                };

                var chartConfig = JsonSerializer.Serialize(config);
                charts.Add(new PdfChartDto
                {
                    Title = $"{chartTitle} - Perf 1 an : {perfText}",
                    Url = $"https://quickchart.io/chart?width=1200&height=420&devicePixelRatio=2&backgroundColor=%23111A2F&c={Uri.EscapeDataString(chartConfig)}"
                });
            }

            return charts;
        }

        private static List<ValuationPoint> BuildFallbackPoints(FinancialSupportAllocation supportAllocation)
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddYears(-1);

            var startValue = supportAllocation.InvestedAmount;
            var endValue = supportAllocation.CurrentAmount;

            if (startValue <= 0m && endValue > 0m)
            {
                startValue = endValue;
            }

            if (endValue <= 0m && startValue > 0m)
            {
                endValue = startValue;
            }

            if (startValue <= 0m && endValue <= 0m)
            {
                startValue = 1m;
                endValue = 1m;
            }

            return new List<ValuationPoint>
            {
                new(startDate, startValue),
                new(endDate, endValue)
            };
        }

        private static List<ValuationPoint> Downsample(List<ValuationPoint> source, int maxPoints)
        {
            if (source.Count <= maxPoints)
            {
                return source;
            }

            var result = new List<ValuationPoint>();
            var step = (double)(source.Count - 1) / (maxPoints - 1);

            for (var i = 0; i < maxPoints; i++)
            {
                var index = (int)Math.Round(i * step);
                index = Math.Clamp(index, 0, source.Count - 1);
                result.Add(source[index]);
            }

            return result
                .DistinctBy(x => x.Date)
                .OrderBy(x => x.Date)
                .ToList();
        }

        private static decimal ComputePerformance(decimal start, decimal end)
        {
            if (start <= 0m)
            {
                return 0m;
            }

            return (end - start) / start;
        }

        private static string FormatSignedPercent(decimal ratio)
        {
            var sign = ratio >= 0m ? "+" : string.Empty;
            return $"{sign}{ratio.ToString("P2", CultureInfo.GetCultureInfo("fr-FR"))}";
        }

        private sealed record ValuationPoint(DateTime Date, decimal Value);

        private static string TranslateOperationType(OperationType type)
        {
            return type switch
            {
                OperationType.InitialPayment => "Versement initial",
                OperationType.ScheduledPayment => "Versement programmé",
                OperationType.FreePayment => "Versement libre",
                OperationType.ParticipationBenefit => "Participation aux bénéfices",
                OperationType.InterestPayment => "Versement d'intérêts",
                OperationType.CouponDetachment => "Détachement de coupon",
                OperationType.PartialWithdrawal => "Rachat partiel",
                OperationType.ScheduledWithdrawal => "Rachat programmé",
                OperationType.TotalWithdrawal => "Rachat total",
                OperationType.ManagementFee => "Frais de gestion",
                OperationType.OperationFee => "Frais d'opération",
                OperationType.Arbitrage => "Arbitrage",
                OperationType.ScheduledArbitrage => "Arbitrage programmé",
                OperationType.Advance => "Avance",
                OperationType.Succession => "Succession",
                OperationType.Donation => "Donation",
                OperationType.BeneficiaryChange => "Changement de bénéficiaire",
                OperationType.Pledge => "Nantissement",
                OperationType.ConversionToAnnuity => "Conversion en rente",
                _ => type.ToString()
            };
        }

        private static string TranslateOperationStatus(OperationStatus status)
        {
            return status switch
            {
                OperationStatus.Pending => "En attente",
                OperationStatus.Executed => "Exécutée",
                OperationStatus.Cancelled => "Annulée",
                OperationStatus.Failed => "Échouée",
                _ => status.ToString()
            };
        }

        private static string ResolveCompartmentLabel(IReadOnlyDictionary<int, string> compartmentById, int? compartmentId)
        {
            if (!compartmentId.HasValue)
            {
                return "-";
            }

            return compartmentById.TryGetValue(compartmentId.Value, out var label)
                ? label
                : "-";
        }

        private static string FormatMoney(decimal amount, string currency)
        {
            var suffix = string.IsNullOrWhiteSpace(currency) ? string.Empty : $" {currency}";
            return $"{amount:N2}{suffix}";
        }

        private static string FormatPercent(decimal? value)
        {
            return value.HasValue ? $"{value.Value:0.##}%" : "-";
        }

        private static string FormatDate(DateTime date)
        {
            return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
    }
}
