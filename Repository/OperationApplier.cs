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
    /// - Les frais retirent des parts sans diminuer l'investi client
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
                if (alloc.CompartmentId == null)
                    throw new InvalidOperationException(
                        $"CompartmentId manquant dans opération {operation.Id}");

                var shares = alloc.Shares ?? 0m;
                var amount = alloc.Amount ?? 0m;

                if (shares <= 0 || amount <= 0)
                    throw new InvalidOperationException(
                        $"Rachat invalide (shares={shares}, amount={amount})");

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

                // 🔥 CORRECTION : on utilise le montant cash réel
                var investedReduction = amount;

                // 🔹 Mise à jour FSA
                fsa.CurrentShares -= shares;
                fsa.InvestedAmount = Math.Max(0m, fsa.InvestedAmount - investedReduction);

                // 🔹 Mise à jour Holding
                holding.TotalShares -= shares;
                holding.TotalInvested = Math.Max(0m, holding.TotalInvested - investedReduction);

                // 🔒 PRU ne change PAS sur rachat

                // 🔹 Nettoyage si position fermée
                if (holding.TotalShares == 0)
                {
                    holding.TotalInvested = 0m;
                    holding.Pru = 0m;
                    fsa.InvestedAmount = 0m;
                }

                // 🔹 (Optionnel mais recommandé) éviter dérives décimales
                fsa.InvestedAmount = Math.Round(fsa.InvestedAmount, 2);
                holding.TotalInvested = Math.Round(holding.TotalInvested, 2);
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
                    throw new InvalidOperationException("Frais > parts détenues");

                fsa.CurrentShares = Math.Max(0m, fsa.CurrentShares - shares);
                holding.TotalShares = Math.Max(0m, holding.TotalShares - shares);

                if (holding.TotalShares == 0)
                {
                    fsa.CurrentShares = 0m;
                    holding.TotalShares = 0m;
                }

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

                var fsa = await context.Set<FinancialSupportAllocation>()
                    .SingleOrDefaultAsync(f =>
                        f.ContractId == operation.ContractId &&
                        f.SupportId == alloc.SupportId &&
                        f.CompartmentId == alloc.CompartmentId, ct)
                    ?? throw new InvalidOperationException("FSA source introuvable");

                var holding = await context.Set<ContractSupportHolding>()
                    .SingleOrDefaultAsync(h =>
                        h.ContractId == operation.ContractId &&
                        h.SupportId == alloc.SupportId &&
                        h.CompartmentId == alloc.CompartmentId, ct)
                    ?? throw new InvalidOperationException("Holding source introuvable");

                if (shares > holding.TotalShares)
                    throw new InvalidOperationException("Arbitrage > parts détenues");

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
                    // 🔥 même logique que withdrawal
                    var investedReduction = amount;

                    fsa.CurrentShares -= shares;
                    fsa.InvestedAmount = Math.Max(0m, fsa.InvestedAmount - investedReduction);

                    holding.TotalShares -= shares;
                    holding.TotalInvested = Math.Max(0m, holding.TotalInvested - investedReduction);
                }
            }

            // ============================================================
            // 🔺 2. ACHATS (TARGET)
            // ============================================================

            foreach (var alloc in targets)
            {
                if (alloc.CompartmentId == null)
                    throw new InvalidOperationException("CompartmentId manquant");

                var shares = alloc.Shares ?? 0m;
                var amount = alloc.Amount ?? 0m;

                if (shares <= 0 || amount <= 0)
                    throw new InvalidOperationException("Arbitrage TARGET invalide");

                var fsa = await GetOrCreateFsaAsync(operation, alloc, context, ct);
                var holding = await GetOrCreateHoldingAsync(operation, alloc, context, ct);

                fsa.CurrentShares += shares;
                fsa.InvestedAmount += amount;

                holding.TotalShares += shares;
                holding.TotalInvested += amount;

                // 🔥 recalcul PRU comme payment
                holding.Pru = holding.TotalShares > 0
                    ? Math.Round(holding.TotalInvested / holding.TotalShares, 7)
                    : 0m;
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
