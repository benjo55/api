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
    /// - TotalInvested représente le coût de revient restant
    /// - Rachats et frais retirent le coût des parts cédées
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
            {
                if (CanIgnoreAllocations(operation.Type))
                    return;

                throw new InvalidOperationException(
                    $"Aucune allocation pour opération {operation.Id}");
            }

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

                case OperationType.ManagementFee:
                case OperationType.OperationFee:
                    await ApplyFeeAsync(operation, allocations, context, cancellationToken);
                    break;

                case OperationType.Arbitrage:
                case OperationType.ScheduledArbitrage:
                    await ApplyArbitrageAsync(operation, allocations, context, cancellationToken);
                    break;
                default:
                    return;
            }
        }

        private static bool CanIgnoreAllocations(OperationType operationType)
        {
            return operationType is
                OperationType.Advance or
                OperationType.AdvanceRepayment or
                OperationType.Succession or
                OperationType.Donation or
                OperationType.BeneficiaryChange or
                OperationType.Pledge or
                OperationType.ConversionToAnnuity;
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
                fsa.InvestedAmount = RoundBasis(fsa.InvestedAmount + amount);

                holding.TotalShares += shares;
                holding.TotalInvested = RoundBasis(holding.TotalInvested + amount);

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
                if (alloc.CompartmentId == null)
                    throw new InvalidOperationException(
                        $"CompartmentId manquant dans opération {operation.Id}");

                var shares = alloc.Shares ?? 0m;
                var amount = alloc.Amount ?? 0m;

                if (shares <= 0 || amount <= 0)
                    throw new InvalidOperationException(
                        $"Rachat invalide (shares={shares}, amount={amount})");

                var fsa = await FindFsaAsync(
                        operation.ContractId,
                        alloc.SupportId,
                        alloc.CompartmentId,
                        context,
                        ct)
                    ?? throw new InvalidOperationException("FSA introuvable");

                var holding = await FindHoldingAsync(
                        operation.ContractId,
                        alloc.SupportId,
                        alloc.CompartmentId,
                        context,
                        ct)
                    ?? throw new InvalidOperationException("Holding introuvable");

                if (shares > holding.TotalShares)
                    throw new InvalidOperationException("Retrait > parts détenues");

                var investedReduction = CostForShares(
                    holding.TotalInvested,
                    holding.TotalShares,
                    shares);

                // 🔹 Mise à jour FSA
                fsa.CurrentShares -= shares;
                fsa.InvestedAmount = RoundBasis(
                    Math.Max(0m, fsa.InvestedAmount - investedReduction));

                // 🔹 Mise à jour Holding
                holding.TotalShares -= shares;
                holding.TotalInvested = RoundBasis(
                    Math.Max(0m, holding.TotalInvested - investedReduction));

                // 🔒 PRU ne change PAS sur rachat

                // 🔹 Nettoyage si position fermée
                if (holding.TotalShares == 0)
                {
                    holding.TotalInvested = 0m;
                    holding.Pru = 0m;
                    fsa.InvestedAmount = 0m;
                }

                holding.Pru = holding.TotalShares > 0m
                    ? Math.Round(holding.TotalInvested / holding.TotalShares, 7)
                    : 0m;
            }
        }

        // ============================================================
        // FRAIS
        // ============================================================

        private static async Task ApplyFeeAsync(
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
                        $"Frais invalides (shares={shares}, amount={amount})");

                var fsa = await FindFsaAsync(
                        operation.ContractId,
                        alloc.SupportId,
                        alloc.CompartmentId,
                        context,
                        ct)
                    ?? throw new InvalidOperationException("FSA introuvable");

                var holding = await FindHoldingAsync(
                        operation.ContractId,
                        alloc.SupportId,
                        alloc.CompartmentId,
                        context,
                        ct)
                    ?? throw new InvalidOperationException("Holding introuvable");

                if (shares > holding.TotalShares)
                    throw new InvalidOperationException("Frais > parts détenues");

                var costReduction = CostForShares(
                    holding.TotalInvested,
                    holding.TotalShares,
                    shares);

                fsa.CurrentShares = Math.Max(0m, fsa.CurrentShares - shares);
                holding.TotalShares = Math.Max(0m, holding.TotalShares - shares);
                fsa.InvestedAmount = RoundBasis(
                    Math.Max(0m, fsa.InvestedAmount - costReduction));
                holding.TotalInvested = RoundBasis(
                    Math.Max(0m, holding.TotalInvested - costReduction));

                if (holding.TotalShares == 0)
                {
                    fsa.CurrentShares = 0m;
                    holding.TotalShares = 0m;
                    fsa.InvestedAmount = 0m;
                    holding.TotalInvested = 0m;
                }

                holding.Pru = holding.TotalShares > 0m
                    ? Math.Round(holding.TotalInvested / holding.TotalShares, 7)
                    : 0m;

                holding.LastUpdated = DateTime.UtcNow;
            }
        }


        // ============================================================
        // 🔻 3. ARBITRAGES (SOURCE et TARGET)
        // ============================================================



        private static async Task ApplyArbitrageAsync(
            Operation operation,
            List<OperationSupportAllocation> allocations,
            DbContext context,
            CancellationToken ct)
        {
            var sources = allocations.Where(a => a.Flow == OperationFlow.Source).ToList();
            var targets = allocations.Where(a => a.Flow == OperationFlow.Target).ToList();

            if (!sources.Any() || !targets.Any())
                throw new InvalidOperationException("Arbitrage invalide : sources ou targets manquants.");

            var totalTargetAmount = targets.Sum(a => a.Amount ?? 0m);
            if (totalTargetAmount <= 0m)
                throw new InvalidOperationException("Arbitrage TARGET invalide : montant total nul.");

            // ============================================================
            // 🔻 1. VENTES (SOURCE)
            // ============================================================

            foreach (var alloc in sources)
            {
                if (alloc.CompartmentId == null)
                    throw new InvalidOperationException("CompartmentId manquant");

                var shares = alloc.Shares ?? 0m;
                var amount = alloc.Amount ?? 0m;

                if (shares <= 0 || amount <= 0)
                    throw new InvalidOperationException("Arbitrage SOURCE invalide");

                var fsa = await FindFsaAsync(
                        operation.ContractId,
                        alloc.SupportId,
                        alloc.CompartmentId,
                        context,
                        ct)
                    ?? throw new InvalidOperationException("FSA source introuvable");

                var holding = await FindHoldingAsync(
                        operation.ContractId,
                        alloc.SupportId,
                        alloc.CompartmentId,
                        context,
                        ct)
                    ?? throw new InvalidOperationException("Holding source introuvable");

                if (shares > holding.TotalShares)
                    throw new InvalidOperationException("Arbitrage > parts détenues");

                var investedBefore = holding.TotalInvested;
                var sharesBefore = holding.TotalShares;

                var investedReduction = CostForShares(
                    investedBefore,
                    sharesBefore,
                    shares);

                investedReduction = Math.Min(investedReduction, investedBefore);

                // Si on vend la totalité (ou quasi-totalité après snap moteur), liquidation propre
                if (shares >= holding.TotalShares)
                {
                    fsa.CurrentShares = Math.Max(0m, fsa.CurrentShares - shares);
                    fsa.InvestedAmount = 0m;
                    holding.TotalShares = 0m;
                    holding.TotalInvested = 0m;
                    holding.Pru = 0m;

                }
                else
                {
                    fsa.CurrentShares -= shares;
                    fsa.InvestedAmount = RoundBasis(
                        Math.Max(0m, fsa.InvestedAmount - investedReduction));

                    holding.TotalShares -= shares;
                    holding.TotalInvested = RoundBasis(
                        Math.Max(0m, holding.TotalInvested - investedReduction));
                    holding.Pru = holding.TotalShares > 0m
                        ? Math.Round(holding.TotalInvested / holding.TotalShares, 7)
                        : 0m;
                }
            }

            // ============================================================
            // 🔺 2. ACHATS (TARGET)
            // ============================================================

            for (var i = 0; i < targets.Count; i++)
            {
                var alloc = targets[i];
                if (alloc.CompartmentId == null)
                    throw new InvalidOperationException("CompartmentId manquant");

                var shares = alloc.Shares ?? 0m;
                var amount = alloc.Amount ?? 0m;

                if (shares <= 0 || amount <= 0)
                    throw new InvalidOperationException("Arbitrage TARGET invalide");

                var investedAmount = RoundBasis(amount);

                var fsa = await GetOrCreateFsaAsync(operation, alloc, context, ct);
                var holding = await GetOrCreateHoldingAsync(operation, alloc, context, ct);

                fsa.CurrentShares += shares;
                fsa.InvestedAmount = RoundBasis(fsa.InvestedAmount + investedAmount);

                holding.TotalShares += shares;
                holding.TotalInvested = RoundBasis(holding.TotalInvested + investedAmount);

                // 🔥 recalcul PRU comme payment
                holding.Pru = holding.TotalShares > 0
                    ? Math.Round(holding.TotalInvested / holding.TotalShares, 7)
                    : 0m;
            }
        }

        private static decimal CostForShares(
            decimal currentCostBasis,
            decimal currentShares,
            decimal sharesToRemove)
        {
            if (currentCostBasis <= 0m || currentShares <= 0m || sharesToRemove <= 0m)
                return 0m;

            var effectiveShares = Math.Min(sharesToRemove, currentShares);
            return RoundBasis(currentCostBasis * effectiveShares / currentShares);
        }

        private static decimal RoundBasis(decimal value) =>
            Math.Round(value, 7, MidpointRounding.AwayFromZero);

        private static async Task<FinancialSupportAllocation> GetOrCreateFsaAsync(
            Operation operation,
            OperationSupportAllocation alloc,
            DbContext context,
            CancellationToken ct)
        {
            var fsa = await FindFsaAsync(
                operation.ContractId,
                alloc.SupportId,
                alloc.CompartmentId,
                context,
                ct);

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
            var holding = await FindHoldingAsync(
                operation.ContractId,
                alloc.SupportId,
                alloc.CompartmentId,
                context,
                ct);

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

        private static Task<FinancialSupportAllocation?> FindFsaAsync(
            int contractId,
            int supportId,
            int? compartmentId,
            DbContext context,
            CancellationToken ct)
        {
            var tracked = context.Set<FinancialSupportAllocation>().Local
                .FirstOrDefault(f =>
                    f.ContractId == contractId &&
                    f.SupportId == supportId &&
                    f.CompartmentId == compartmentId);

            if (tracked != null)
                return Task.FromResult<FinancialSupportAllocation?>(tracked);

            return context.Set<FinancialSupportAllocation>()
                .SingleOrDefaultAsync(f =>
                    f.ContractId == contractId &&
                    f.SupportId == supportId &&
                    f.CompartmentId == compartmentId, ct);
        }

        private static Task<ContractSupportHolding?> FindHoldingAsync(
            int contractId,
            int supportId,
            int? compartmentId,
            DbContext context,
            CancellationToken ct)
        {
            var tracked = context.Set<ContractSupportHolding>().Local
                .FirstOrDefault(h =>
                    h.ContractId == contractId &&
                    h.SupportId == supportId &&
                    h.CompartmentId == compartmentId);

            if (tracked != null)
                return Task.FromResult<ContractSupportHolding?>(tracked);

            return context.Set<ContractSupportHolding>()
                .SingleOrDefaultAsync(h =>
                    h.ContractId == contractId &&
                    h.SupportId == supportId &&
                    h.CompartmentId == compartmentId, ct);
        }
    }

}
