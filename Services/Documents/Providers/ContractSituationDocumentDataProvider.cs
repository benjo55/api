using System.Globalization;
using System.Text.Json;
using api.Dtos.Documents;
using api.Interfaces;
using api.Interfaces.Documents;
using api.Models;
using api.Services.Documents.Models;

namespace api.Services.Documents.Providers
{
    public sealed class ContractSituationDocumentDataProvider : IDocumentDataProvider
    {
        private readonly IContractRepository _contractRepository;
        private readonly IOperationRepository _operationRepository;

        public ContractSituationDocumentDataProvider(
            IContractRepository contractRepository,
            IOperationRepository operationRepository)
        {
            _contractRepository = contractRepository;
            _operationRepository = operationRepository;
        }

        public async Task<object> BuildModelAsync(
            DocumentDefinition definition,
            GenerateDocumentRequestDto request,
            DocumentGenerationContext context,
            CancellationToken cancellationToken = default)
        {
            if (!int.TryParse(request.SubjectId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var contractId))
            {
                throw new InvalidOperationException("L'identifiant du contrat est obligatoire.");
            }

            var contract = await _contractRepository.LoadContractById(contractId)
                ?? throw new KeyNotFoundException("Contrat introuvable.");
            var operations = (await _operationRepository.GetByContractAsync(contractId))
                .OrderByDescending(x => x.OperationDate)
                .Take(ReadIntParameter(request, "recentOperationsLimit", 12))
                .ToList();
            var asOfDate = ReadDateParameter(request, "asOfDate") ?? DateTime.UtcNow.Date;
            var compartmentById = contract.Compartments.ToDictionary(x => x.Id, x => x.Label);

            return new ContractSituationDocumentModel(
                contract.Id,
                contract.ContractNumber,
                contract.ContractLabel,
                BuildPersonName(contract.Person),
                contract.Product?.ProductName ?? contract.Product?.CommercialName ?? "-",
                contract.Product?.Insurer?.Name ?? "-",
                asOfDate,
                string.IsNullOrWhiteSpace(contract.Currency) ? "EUR" : contract.Currency,
                contract.CurrentValue,
                contract.TotalPayments == 0 ? contract.PaidExecuted : contract.TotalPayments,
                contract.TotalWithdrawals == 0 ? contract.WithdrawnExecuted : contract.TotalWithdrawals,
                contract.NetInvested,
                contract.PerformancePercent,
                contract.Supports
                    .OrderByDescending(x => x.CurrentAmount)
                    .Select(x => new ContractSituationSupportLine(
                        x.Support?.Label ?? $"Support #{x.SupportId}",
                        compartmentById.TryGetValue(x.CompartmentId, out var compartment) ? compartment : "-",
                        x.InvestedAmount,
                        x.CurrentAmount,
                        x.CurrentShares,
                        x.AllocationPercentage,
                        x.Support?.LastValuationAmount,
                        x.Support?.LastValuationDate,
                        x.Performance))
                    .ToList(),
                operations
                    .Select(x => new ContractSituationOperationLine(
                        x.OperationDate,
                        x.ExecutionDate,
                        TranslateOperationType(x.Type),
                        TranslateOperationStatus(x.Status),
                        x.ExecutedAmount ?? x.Amount,
                        string.IsNullOrWhiteSpace(x.Currency) ? contract.Currency : x.Currency))
                    .ToList());
        }

        private static int ReadIntParameter(GenerateDocumentRequestDto request, string propertyName, int fallback)
        {
            if (!request.Parameters.HasValue ||
                request.Parameters.Value.ValueKind != JsonValueKind.Object ||
                !request.Parameters.Value.TryGetProperty(propertyName, out var value) ||
                !value.TryGetInt32(out var parsed))
            {
                return fallback;
            }

            return Math.Clamp(parsed, 1, 50);
        }

        private static DateTime? ReadDateParameter(GenerateDocumentRequestDto request, string propertyName)
        {
            if (!request.Parameters.HasValue ||
                request.Parameters.Value.ValueKind != JsonValueKind.Object ||
                !request.Parameters.Value.TryGetProperty(propertyName, out var value))
            {
                return null;
            }

            return DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed)
                ? parsed.Date
                : null;
        }

        private static string BuildPersonName(Person? person)
        {
            if (person is null)
            {
                return "Titulaire non renseigné";
            }

            return string.Join(" ", new[] { person.FirstName, person.LastName }.Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        }

        private static string TranslateOperationType(OperationType type) => type switch
        {
            OperationType.InitialPayment => "Versement initial",
            OperationType.ScheduledPayment => "Versement programmé",
            OperationType.FreePayment => "Versement libre",
            OperationType.PartialWithdrawal => "Rachat partiel",
            OperationType.ScheduledWithdrawal => "Rachat programmé",
            OperationType.TotalWithdrawal => "Rachat total",
            OperationType.Arbitrage => "Arbitrage",
            OperationType.ManagementFee => "Frais de gestion",
            OperationType.Advance => "Avance",
            OperationType.AdvanceRepayment => "Remboursement d'avance",
            _ => type.ToString()
        };

        private static string TranslateOperationStatus(OperationStatus status) => status switch
        {
            OperationStatus.Pending => "En attente",
            OperationStatus.Executed => "Exécutée",
            OperationStatus.Cancelled => "Annulée",
            OperationStatus.Failed => "Échouée",
            _ => status.ToString()
        };
    }
}
