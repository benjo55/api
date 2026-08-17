using api.Interfaces;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Services.EuroFunds
{
    public sealed class EuroFundLotService : IEuroFundLotService
    {
        private const decimal AmountTolerance = 0.0000001m;
        private readonly IEuroFundValueDateService _valueDateService;

        public EuroFundLotService()
            : this(new EuroFundValueDateService())
        {
        }

        public EuroFundLotService(IEuroFundValueDateService valueDateService)
        {
            _valueDateService = valueDateService;
        }

        public async Task ApplyOperationAsync(
            Operation operation,
            DbContext context,
            CancellationToken cancellationToken = default)
        {
            if (operation.Status != OperationStatus.Executed)
                return;

            var allocations = operation.Allocations?.ToList()
                ?? await context.Set<OperationSupportAllocation>()
                    .Include(a => a.Support)
                    .Where(a => a.OperationId == operation.Id)
                    .ToListAsync(cancellationToken);

            if (!allocations.Any())
                return;

            var euroFundAllocations = new List<OperationSupportAllocation>();

            foreach (var allocation in allocations)
            {
                var support = allocation.Support
                    ?? await context.Set<FinancialSupport>().FindAsync([allocation.SupportId], cancellationToken);

                if (support?.SupportNature != FinancialSupportNature.EuroFund)
                    continue;

                euroFundAllocations.Add(allocation);
            }

            foreach (var group in euroFundAllocations.GroupBy(a => new { a.SupportId, a.Flow }))
            {
                var amount = Math.Round(group.Sum(a => a.Amount ?? 0m), 7, MidpointRounding.AwayFromZero);
                if (amount <= 0m)
                    continue;

                var allocation = group.First();

                if (IsEuroFundIncrease(operation.Type, allocation.Flow))
                {
                    await CreateLotAsync(operation, allocation, amount, context, cancellationToken);
                }
                else if (IsEuroFundDecrease(operation.Type, allocation.Flow))
                {
                    await ConsumeLotsAsync(operation, allocation, amount, EuroFundLotMovementType.Out, context, cancellationToken);
                }
            }
        }

        private static bool IsEuroFundIncrease(OperationType type, OperationFlow? flow)
        {
            if (type is OperationType.Arbitrage or OperationType.ScheduledArbitrage)
                return flow == OperationFlow.Target;

            return type is OperationType.InitialPayment
                or OperationType.FreePayment
                or OperationType.ScheduledPayment
                or OperationType.ParticipationBenefit
                or OperationType.InterestPayment
                or OperationType.CouponDetachment;
        }

        private static bool IsEuroFundDecrease(OperationType type, OperationFlow? flow)
        {
            if (type is OperationType.Arbitrage or OperationType.ScheduledArbitrage)
                return flow == OperationFlow.Source;

            return type is OperationType.PartialWithdrawal
                or OperationType.TotalWithdrawal
                or OperationType.ScheduledWithdrawal
                or OperationType.ManagementFee
                or OperationType.OperationFee;
        }

        private async Task CreateLotAsync(
            Operation operation,
            OperationSupportAllocation allocation,
            decimal amount,
            DbContext context,
            CancellationToken ct)
        {
            var existingLot = context.Set<EuroFundLot>().Local
                .FirstOrDefault(l =>
                    l.SourceOperationId == operation.Id &&
                    l.ContractId == operation.ContractId &&
                    l.FinancialSupportId == allocation.SupportId);

            existingLot ??= await context.Set<EuroFundLot>()
                .FirstOrDefaultAsync(l =>
                    l.SourceOperationId == operation.Id &&
                    l.ContractId == operation.ContractId &&
                    l.FinancialSupportId == allocation.SupportId,
                    ct);

            if (existingLot != null)
                return;

            var settings = await context.Set<EuroFundConfiguration>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.FinancialSupportId == allocation.SupportId, ct);
            var valueDate = _valueDateService.ComputeValueDate(operation.OperationDate, settings);

            var lot = new EuroFundLot
            {
                ContractId = operation.ContractId,
                FinancialSupportId = allocation.SupportId,
                SourceOperationId = operation.Id,
                InitialAmount = amount,
                RemainingAmount = amount,
                ValueDate = valueDate,
                CreatedAt = DateTime.UtcNow,
            };

            context.Set<EuroFundLot>().Add(lot);
            context.Set<EuroFundLotMovement>().Add(new EuroFundLotMovement
            {
                EuroFundLot = lot,
                ContractId = operation.ContractId,
                FinancialSupportId = allocation.SupportId,
                OperationId = operation.Id,
                MovementDate = valueDate,
                Amount = amount,
                MovementType = operation.Type == OperationType.ParticipationBenefit
                    ? EuroFundLotMovementType.ProfitParticipation
                    : EuroFundLotMovementType.In,
                CreatedAt = DateTime.UtcNow,
            });
        }

        private static async Task ConsumeLotsAsync(
            Operation operation,
            OperationSupportAllocation allocation,
            decimal amount,
            EuroFundLotMovementType movementType,
            DbContext context,
            CancellationToken ct)
        {
            var lots = await context.Set<EuroFundLot>()
                .Where(l =>
                    l.ContractId == operation.ContractId &&
                    l.FinancialSupportId == allocation.SupportId &&
                    l.RemainingAmount > AmountTolerance)
                .OrderBy(l => l.ValueDate)
                .ThenBy(l => l.Id)
                .ToListAsync(ct);

            if (!lots.Any())
                throw new InvalidOperationException(
                    $"Aucun lot fonds euros disponible pour contractId={operation.ContractId}, supportId={allocation.SupportId}.");

            if (operation.Type == OperationType.TotalWithdrawal)
                amount = lots.Sum(l => l.RemainingAmount);

            var totalRemaining = lots.Sum(l => l.RemainingAmount);
            if (amount > totalRemaining + AmountTolerance)
                throw new InvalidOperationException(
                    $"Sortie fonds euros {amount} supérieure aux lots restants {totalRemaining}.");

            var consumed = 0m;
            for (var i = 0; i < lots.Count; i++)
            {
                var lot = lots[i];
                var share = i == lots.Count - 1
                    ? amount - consumed
                    : Math.Round(amount * lot.RemainingAmount / totalRemaining, 7, MidpointRounding.AwayFromZero);

                share = Math.Min(share, lot.RemainingAmount);
                if (share <= 0m)
                    continue;

                lot.RemainingAmount = Math.Round(lot.RemainingAmount - share, 7, MidpointRounding.AwayFromZero);
                if (lot.RemainingAmount < AmountTolerance)
                    lot.RemainingAmount = 0m;

                consumed += share;
                context.Set<EuroFundLotMovement>().Add(new EuroFundLotMovement
                {
                    EuroFundLotId = lot.Id,
                    ContractId = operation.ContractId,
                    FinancialSupportId = allocation.SupportId,
                    OperationId = operation.Id,
                    MovementDate = operation.OperationDate.Date,
                    Amount = -share,
                    MovementType = movementType,
                    CreatedAt = DateTime.UtcNow,
                });
            }
        }
    }
}
