using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    /// <summary>
    /// Applique les effets financiers des opérations EXECUTÉES
    /// sur les holdings.
    ///
    /// 🔒 RÈGLES FONDAMENTALES :
    /// - Les parts sont la source de vérité
    /// - Le PRU est recalculé uniquement sur versement
    /// - Le PRU ne change jamais sur rachat
    /// - TotalInvested diminue proportionnellement lors d’un rachat
    /// - Aucune valorisation (CurrentAmount) n’est gérée ici
    /// </summary>
    public sealed class OperationApplier : IOperationApplier
    {
        public async Task ApplyAsync(
            Operation operation,
            DbContext context,
            CancellationToken cancellationToken = default)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            if (operation.Status != OperationStatus.Executed)
                throw new InvalidOperationException(
                    $"Operation {operation.Id} non exécutée.");

            var allocations = operation.Allocations?.ToList()
                ?? await context.Set<OperationSupportAllocation>()
                    .Where(a => a.OperationId == operation.Id)
                    .ToListAsync(cancellationToken);

            if (!allocations.Any())
                throw new InvalidOperationException(
                    $"Aucune allocation pour opération {operation.Id}");

            switch (operation.Type)
            {
                case OperationType.InitialPayment:
                case OperationType.FreePayment:
                case OperationType.ScheduledPayment:
                    await ApplyPaymentAsync(operation, allocations, context, cancellationToken);
                    break;

                case OperationType.PartialWithdrawal:
                case OperationType.TotalWithdrawal:
                case OperationType.ScheduledWithdrawal:
                    await ApplyWithdrawalAsync(operation, allocations, context, cancellationToken);
                    break;

                default:
                    return;
            }
        }

        // ============================================================
        // VERSEMENTS
        // ============================================================

        private static async Task ApplyPaymentAsync(
            Operation operation,
            List<OperationSupportAllocation> allocations,
            DbContext context,
            CancellationToken ct)
        {
            foreach (var alloc in allocations)
            {
                if (alloc.CompartmentId == null)
                    throw new InvalidOperationException(
                        $"CompartmentId manquant dans opération {operation.Id}");

                var shares = alloc.Shares ?? 0m;
                var amount = alloc.Amount ?? 0m;

                if (shares <= 0 || amount <= 0)
                    throw new InvalidOperationException(
                        $"Versement invalide (shares={shares}, amount={amount})");

                var fsa = await GetOrCreateFsaAsync(operation, alloc, context, ct);
                var holding = await GetOrCreateHoldingAsync(operation, alloc, context, ct);

                fsa.CurrentShares += shares;
                fsa.InvestedAmount += amount;

                holding.TotalShares += shares;
                holding.TotalInvested += amount;

                holding.Pru = holding.TotalShares > 0
                    ? Math.Round(holding.TotalInvested / holding.TotalShares, 7)
                    : 0m;
            }
        }

        // ============================================================
        // RACHATS
        // ============================================================

        private static async Task ApplyWithdrawalAsync(
            Operation operation,
            List<OperationSupportAllocation> allocations,
            DbContext context,
            CancellationToken ct)
        {
            foreach (var alloc in allocations)
            {
                if (alloc.CompartmentId < 0)
                    throw new InvalidOperationException(
                        $"CompartmentId manquant dans opération {operation.Id}");

                var shares = alloc.Shares ?? 0m;
                if (shares <= 0)
                    throw new InvalidOperationException("Shares invalides");

                var fsa = await context.Set<FinancialSupportAllocation>()
                    .SingleOrDefaultAsync(f =>
                        f.ContractId == operation.ContractId &&
                        f.SupportId == alloc.SupportId &&
                        f.CompartmentId == alloc.CompartmentId, ct)
                    ?? throw new InvalidOperationException("FSA introuvable");

                var holding = await context.Set<ContractSupportHolding>()
                    .SingleOrDefaultAsync(h =>
                        h.ContractId == operation.ContractId &&
                        h.SupportId == alloc.SupportId &&
                        h.CompartmentId == alloc.CompartmentId, ct)
                    ?? throw new InvalidOperationException("Holding introuvable");

                if (shares > holding.TotalShares)
                    throw new InvalidOperationException("Retrait > parts détenues");

                var investedReduction = Math.Round(shares * holding.Pru, 7);

                fsa.CurrentShares -= shares;
                fsa.InvestedAmount = Math.Max(0m, fsa.InvestedAmount - investedReduction);

                holding.TotalShares -= shares;
                holding.TotalInvested = Math.Max(0m, holding.TotalInvested - investedReduction);

                if (holding.TotalShares == 0)
                {
                    holding.TotalInvested = 0m;
                    holding.Pru = 0m;
                }
            }
        }

        private static async Task<FinancialSupportAllocation> GetOrCreateFsaAsync(
            Operation operation,
            OperationSupportAllocation alloc,
            DbContext context,
            CancellationToken ct)
        {
            var fsa = await context.Set<FinancialSupportAllocation>()
                .SingleOrDefaultAsync(f =>
                    f.ContractId == operation.ContractId &&
                    f.SupportId == alloc.SupportId &&
                    f.CompartmentId == alloc.CompartmentId, ct);

            if (fsa != null)
                return fsa;

            fsa = new FinancialSupportAllocation
            {
                ContractId = operation.ContractId,
                SupportId = alloc.SupportId,
                CompartmentId = alloc.CompartmentId!.Value,
                CurrentShares = 0m,
                InvestedAmount = 0m
            };

            context.Set<FinancialSupportAllocation>().Add(fsa);
            return fsa;
        }

        private static async Task<ContractSupportHolding> GetOrCreateHoldingAsync(
            Operation operation,
            OperationSupportAllocation alloc,
            DbContext context,
            CancellationToken ct)
        {
            var holding = await context.Set<ContractSupportHolding>()
                .SingleOrDefaultAsync(h =>
                    h.ContractId == operation.ContractId &&
                    h.SupportId == alloc.SupportId &&
                    h.CompartmentId == alloc.CompartmentId, ct);

            if (holding != null)
                return holding;

            holding = new ContractSupportHolding
            {
                ContractId = operation.ContractId,
                SupportId = alloc.SupportId,
                CompartmentId = alloc.CompartmentId!.Value,
                TotalShares = 0m,
                TotalInvested = 0m,
                Pru = 0m
            };

            context.Set<ContractSupportHolding>().Add(holding);
            return holding;
        }
    }

}
