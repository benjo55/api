using api.Data;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    /// <summary>
    /// Rejoue le journal exécuté pour reconstruire le coût de revient restant.
    /// Les flux externes du contrat restent portés par Contract.NetInvested.
    /// </summary>
    public sealed class CostBasisService : ICostBasisService
    {
        private const decimal ShareTolerance = 0.0000001m;
        private readonly ApplicationDBContext _context;

        private sealed class Position
        {
            public decimal Shares { get; set; }
            public decimal CostBasis { get; set; }
        }

        public CostBasisService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<CostBasisRebuildResult> RebuildAsync(int contractId)
        {
            var operations = await _context.Operations
                .AsNoTracking()
                .Where(o => o.ContractId == contractId && o.Status == OperationStatus.Executed)
                .Include(o => o.Allocations)
                .OrderBy(o => o.ExecutionDate ?? o.OperationDate)
                .ThenBy(o => o.Id)
                .ToListAsync();

            var positions = new Dictionary<(int SupportId, int CompartmentId), Position>();

            Position GetPosition(OperationSupportAllocation allocation)
            {
                if (allocation.CompartmentId is not > 0)
                    throw new InvalidOperationException(
                        $"Allocation {allocation.Id} sans poche pour le contrat {contractId}.");

                var key = (allocation.SupportId, allocation.CompartmentId.Value);
                if (!positions.TryGetValue(key, out var position))
                {
                    position = new Position();
                    positions[key] = position;
                }

                return position;
            }

            static decimal RemoveCost(Position position, decimal shares)
            {
                if (shares <= 0m || position.Shares <= 0m)
                    return 0m;

                var effectiveShares = Math.Min(shares, position.Shares);
                var removed = position.CostBasis * (effectiveShares / position.Shares);
                position.Shares -= effectiveShares;
                position.CostBasis -= removed;

                if (position.Shares <= ShareTolerance)
                {
                    position.Shares = 0m;
                    position.CostBasis = 0m;
                }

                return removed;
            }

            foreach (var operation in operations)
            {
                var rows = operation.Allocations.Where(a => a.Shares is > 0m).ToList();

                if (operation.Type is OperationType.Arbitrage or OperationType.ScheduledArbitrage)
                {
                    foreach (var allocation in rows.Where(a => a.Flow == OperationFlow.Source))
                        RemoveCost(GetPosition(allocation), allocation.Shares ?? 0m);

                    foreach (var allocation in rows.Where(a => a.Flow == OperationFlow.Target))
                    {
                        var position = GetPosition(allocation);
                        position.Shares += allocation.Shares ?? 0m;
                        position.CostBasis += allocation.Amount ?? 0m;
                    }

                    continue;
                }

                foreach (var allocation in rows)
                {
                    var position = GetPosition(allocation);

                    if (operation.Type is OperationType.InitialPayment or
                        OperationType.FreePayment or
                        OperationType.ScheduledPayment)
                    {
                        position.Shares += allocation.Shares ?? 0m;
                        position.CostBasis += allocation.Amount ?? 0m;
                    }
                    else if (operation.Type is OperationType.PartialWithdrawal or
                             OperationType.TotalWithdrawal or
                             OperationType.ScheduledWithdrawal or
                             OperationType.ManagementFee or
                             OperationType.OperationFee)
                    {
                        RemoveCost(position, allocation.Shares ?? 0m);
                    }
                }
            }

            var fsas = await _context.FinancialSupportAllocations
                .Where(f => f.ContractId == contractId)
                .ToListAsync();
            var holdings = await _context.ContractSupportHoldings
                .Where(h => h.ContractId == contractId)
                .ToListAsync();

            decimal maxShareDelta = 0m;

            foreach (var fsa in fsas)
            {
                var key = (fsa.SupportId, fsa.CompartmentId);
                positions.TryGetValue(key, out var replayed);
                var costBasis = Math.Round(replayed?.CostBasis ?? 0m, 7, MidpointRounding.AwayFromZero);
                var replayedShares = replayed?.Shares ?? 0m;
                maxShareDelta = Math.Max(maxShareDelta, Math.Abs(fsa.CurrentShares - replayedShares));
                fsa.InvestedAmount = costBasis;
            }

            foreach (var holding in holdings)
            {
                var key = (holding.SupportId, holding.CompartmentId);
                positions.TryGetValue(key, out var replayed);
                var costBasis = Math.Round(replayed?.CostBasis ?? 0m, 7, MidpointRounding.AwayFromZero);
                holding.TotalInvested = costBasis;
                holding.Pru = holding.TotalShares > 0m
                    ? Math.Round(costBasis / holding.TotalShares, 7, MidpointRounding.AwayFromZero)
                    : 0m;
            }

            if (maxShareDelta > ShareTolerance)
            {
                throw new InvalidOperationException(
                    $"Rejeu des parts incohérent pour le contrat {contractId}: écart maximal {maxShareDelta}.");
            }

            await _context.SaveChangesAsync();

            return new CostBasisRebuildResult(
                contractId,
                positions.Count,
                Math.Round(fsas.Sum(f => f.InvestedAmount), 7),
                maxShareDelta);
        }
    }
}
