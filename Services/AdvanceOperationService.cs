using api.Data;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class AdvanceOperationService : IAdvanceOperationService
    {
        private readonly ApplicationDBContext _context;
        private readonly IAdvanceRepository _advanceRepository;

        public AdvanceOperationService(
            ApplicationDBContext context,
            IAdvanceRepository advanceRepository)
        {
            _context = context;
            _advanceRepository = advanceRepository;
        }

        public async Task ValidateForCreationAsync(Operation operation)
        {
            if (!IsAdvanceOperation(operation.Type))
                return;

            var amount = operation.Amount ?? 0m;
            if (amount <= 0m)
                throw new BusinessException("Le montant de l'opération d'avance doit être supérieur à zéro.");

            var detail = operation.AdvanceDetail;
            if (detail?.AdvanceId is not > 0)
                throw new BusinessException("Une avance existante doit être sélectionnée.");

            var advance = await _context.Advances
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == detail.AdvanceId.Value)
                ?? throw new BusinessException($"Avance {detail.AdvanceId.Value} introuvable.");

            ValidateCommon(operation, advance);

            if (operation.Type == OperationType.Advance)
            {
                await ValidateDisbursementAsync(operation, advance, amount);
                detail.TransactionType = AdvanceTransactionType.Disbursement;
                return;
            }

            if (advance.Status is not (AdvanceStatus.Active or AdvanceStatus.Renewed) ||
                advance.OutstandingCapital <= 0m)
            {
                throw new BusinessException("Seule une avance active avec un encours peut être remboursée.");
            }

            var repaymentType = ResolveRepaymentType(detail.TransactionType, amount, advance.OutstandingCapital);
            detail.TransactionType = repaymentType;

            var pendingAmount = await _context.Operations
                .Where(o =>
                    o.Id != operation.Id &&
                    o.Type == OperationType.AdvanceRepayment &&
                    o.Status == OperationStatus.Pending &&
                    o.AdvanceDetail != null &&
                    o.AdvanceDetail.AdvanceId == advance.Id)
                .SumAsync(o => o.Amount ?? 0m);

            var availableToRepay = Math.Max(0m, advance.OutstandingCapital - pendingAmount);
            if (amount > availableToRepay)
            {
                throw new BusinessException(
                    $"Le remboursement dépasse l'encours disponible ({availableToRepay:F2} EUR).");
            }

            if (repaymentType == AdvanceTransactionType.TotalRepayment && amount != availableToRepay)
            {
                throw new BusinessException(
                    $"Un remboursement total doit être égal à l'encours disponible ({availableToRepay:F2} EUR).");
            }
        }

        public async Task ApplyAsync(Operation operation)
        {
            if (!IsAdvanceOperation(operation.Type))
                return;

            if (await _context.AdvanceTransactions.AnyAsync(t => t.OperationId == operation.Id))
                return;

            var detail = operation.AdvanceDetail;
            if (detail?.AdvanceId is not > 0)
                throw new BusinessException("L'opération n'est liée à aucune avance.");

            var advance = await _context.Advances
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.Id == detail.AdvanceId.Value)
                ?? throw new BusinessException($"Avance {detail.AdvanceId.Value} introuvable.");

            ValidateCommon(operation, advance);

            var amount = operation.Amount ?? 0m;
            if (amount <= 0m)
                throw new BusinessException("Le montant de l'opération d'avance doit être supérieur à zéro.");

            AdvanceTransactionType transactionType;

            if (operation.Type == OperationType.Advance)
            {
                await ValidateDisbursementAsync(operation, advance, amount);
                transactionType = AdvanceTransactionType.Disbursement;
                advance.OutstandingCapital += amount;
                advance.DisbursementDate ??= operation.OperationDate;
                advance.Status = AdvanceStatus.Active;
            }
            else
            {
                transactionType = ResolveRepaymentType(
                    detail.TransactionType,
                    amount,
                    advance.OutstandingCapital);

                if (advance.Status is not (AdvanceStatus.Active or AdvanceStatus.Renewed))
                    throw new BusinessException("Seule une avance active peut être remboursée.");

                if (amount > advance.OutstandingCapital)
                    throw new BusinessException("Le remboursement dépasse le capital restant dû.");

                if (transactionType == AdvanceTransactionType.TotalRepayment &&
                    amount != advance.OutstandingCapital)
                {
                    throw new BusinessException(
                        $"Un remboursement total doit être égal au capital restant ({advance.OutstandingCapital:F2} EUR).");
                }

                advance.OutstandingCapital -= amount;
                if (transactionType == AdvanceTransactionType.TotalRepayment ||
                    advance.OutstandingCapital <= 0m)
                {
                    advance.OutstandingCapital = 0m;
                    advance.Status = AdvanceStatus.Settled;
                    transactionType = AdvanceTransactionType.TotalRepayment;
                }
            }

            detail.TransactionType = transactionType;
            advance.UpdatedDate = DateTime.UtcNow;
            advance.Transactions.Add(new AdvanceTransaction
            {
                AdvanceId = advance.Id,
                OperationId = operation.Id,
                OperationDate = operation.OperationDate,
                Type = transactionType,
                Amount = amount,
                Comment = detail.Comment,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            });
        }

        private async Task ValidateDisbursementAsync(
            Operation operation,
            Advance advance,
            decimal amount)
        {
            if (advance.Status != AdvanceStatus.Approved)
                throw new BusinessException("Seule une avance approuvée peut être décaissée.");

            var approvedAmount = advance.ApprovedAmount ?? 0m;
            if (approvedAmount <= 0m || amount > approvedAmount)
                throw new BusinessException($"Le décaissement dépasse le montant approuvé ({approvedAmount:F2} EUR).");

            var duplicateExists = await _context.Operations.AnyAsync(o =>
                o.Id != operation.Id &&
                o.Type == OperationType.Advance &&
                o.Status != OperationStatus.Cancelled &&
                o.Status != OperationStatus.Failed &&
                o.AdvanceDetail != null &&
                o.AdvanceDetail.AdvanceId == advance.Id);

            if (duplicateExists)
                throw new BusinessException("Une opération de décaissement existe déjà pour cette avance.");

            var eligibility = await _advanceRepository.GetEligibilityAsync(advance.ContractId)
                ?? throw new BusinessException("Impossible de calculer l'éligibilité de l'avance.");

            if (amount > eligibility.AvailableAdvanceAmount)
            {
                throw new BusinessException(
                    $"Le décaissement dépasse le plafond disponible ({eligibility.AvailableAdvanceAmount:F2} EUR).");
            }
        }

        private static void ValidateCommon(Operation operation, Advance advance)
        {
            if (advance.ContractId != operation.ContractId)
                throw new BusinessException("L'avance sélectionnée n'appartient pas au contrat de l'opération.");

            if (advance.Locked)
                throw new BusinessException("Cette avance est verrouillée.");

            if (advance.Status is AdvanceStatus.Cancelled or AdvanceStatus.Settled)
                throw new BusinessException("Cette avance est clôturée.");
        }

        private static AdvanceTransactionType ResolveRepaymentType(
            AdvanceTransactionType? requestedType,
            decimal amount,
            decimal outstanding)
        {
            if (requestedType is not null &&
                requestedType is not (AdvanceTransactionType.PartialRepayment or AdvanceTransactionType.TotalRepayment))
            {
                throw new BusinessException("Le type de remboursement est invalide.");
            }

            return requestedType ??
                (amount == outstanding
                    ? AdvanceTransactionType.TotalRepayment
                    : AdvanceTransactionType.PartialRepayment);
        }

        private static bool IsAdvanceOperation(OperationType type)
            => type is OperationType.Advance or OperationType.AdvanceRepayment;
    }
}
