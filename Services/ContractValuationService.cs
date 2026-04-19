using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;

public interface IContractValuationService
{
    Task<decimal> ComputeContractValueAsync(int contractId);
    Task<int> UpdateFsaAmountsForSupportAsync(int supportId);
}

public class ContractValuationService : IContractValuationService
{
    private readonly ApplicationDBContext _context;
    private readonly ILogger<ContractValuationService> _logger;

    public ContractValuationService(
        ApplicationDBContext context,
        ILogger<ContractValuationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==========================================================
    // 🔥 ComputeContractValueAsync : VALORISATION COMPLETE
    // ==========================================================
    public async Task<decimal> ComputeContractValueAsync(int contractId)
    {
        _logger.LogInformation("➡️ Début ComputeContractValueAsync pour contrat {ContractId}", contractId);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1️⃣ Charger contrat + compartiments
            var contract = await _context.Contracts
                .Include(c => c.Compartments)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                _logger.LogWarning("❌ Contrat {ContractId} introuvable.", contractId);
                return 0m;
            }

            // 2️⃣ Charger holdings consolidés (PRU / perf uniquement)
            var holdings = await _context.ContractSupportHoldings
                .Include(h => h.Support)
                .Where(h => h.ContractId == contractId)
                .ToListAsync();

            foreach (var h in holdings)
            {
                _logger.LogWarning(
                    "Holding trouvé → Support={SupportId}, Compartment={CompartmentId}, Shares={Shares}",
                    h.SupportId,
                    h.CompartmentId,
                    h.TotalShares
                );
            }

            // 3️⃣ Mettre à jour les montants holdings (DERIVÉ)
            foreach (var h in holdings)
            {
                decimal vl = h.Support?.LastValuationAmount ?? 0m;

                // 1️⃣ Valeur courante
                h.CurrentAmount = Math.Round(h.TotalShares * vl, 7);

                // 2️⃣ Performance %
                if (h.TotalInvested > 0)
                {
                    var perf = ((h.CurrentAmount ?? 0m) - h.TotalInvested)
                               / h.TotalInvested * 100m;

                    h.PerformancePercent = Math.Round(perf, 4, MidpointRounding.AwayFromZero);
                }
                else
                {
                    h.PerformancePercent = 0m;
                }
                _logger.LogWarning(
                    "Holding Support={SupportId} Compartment={CompartmentId} Perf={Perf}",
                    h.SupportId,
                    h.CompartmentId,
                    h.PerformancePercent
                );
                _logger.LogWarning("Before SaveChanges Perf state = {State}", _context.Entry(h).Property(x => x.PerformancePercent).IsModified);

                h.LastUpdated = DateTime.UtcNow;

            }

            await _context.SaveChangesAsync();

            // 4️⃣ Charger TOUTES les FSA
            var fsas = await _context.FinancialSupportAllocations
                .Where(f => f.ContractId == contractId)
                .Include(f => f.Support)
                .ToListAsync();

            // 5️⃣ 🔥 Recalcul FSA.CurrentAmount = Shares × VL (SOURCE DE VÉRITÉ)
            foreach (var fsa in fsas)
            {
                decimal vl = fsa.Support?.LastValuationAmount ?? 0m;
                fsa.CurrentAmount = Math.Round(fsa.CurrentShares * vl, 7);
                // ⚠️ InvestedAmount JAMAIS modifié ici
            }

            await _context.SaveChangesAsync();

            // 6️⃣ 🔥 Valorisation des compartiments = somme FSA
            foreach (var comp in contract.Compartments)
            {
                comp.CurrentValue = Math.Round(
                    fsas.Where(f => f.CompartmentId == comp.Id)
                        .Sum(f => f.CurrentAmount),
                    7
                );
            }

            // 7️⃣ 🔥 Valeur contrat = somme des compartiments (FSA)
            decimal totalContractValue = contract.Compartments.Sum(c => c.CurrentValue);

            // 8️⃣ Mise à jour des flux du contrat
            var allOps = await _context.Operations
                .Where(o => o.ContractId == contractId)
                .Select(o => new { o.Type, o.Amount })
                .ToListAsync();

            decimal initialPremium = allOps
                .Where(o => o.Type == OperationType.InitialPayment)
                .Sum(o => o.Amount ?? 0m);

            decimal totalPayments = allOps
                .Where(o => o.Type == OperationType.FreePayment ||
                            o.Type == OperationType.ScheduledPayment)
                .Sum(o => o.Amount ?? 0m);

            decimal totalWithdrawals = allOps
                .Where(o => o.Type == OperationType.PartialWithdrawal ||
                            o.Type == OperationType.TotalWithdrawal ||
                            o.Type == OperationType.ScheduledWithdrawal)
                .Sum(o => o.Amount ?? 0m);

            decimal netInvested = initialPremium + totalPayments - totalWithdrawals;

            // 9️⃣ KPIs contrat
            contract.InitialPremium = initialPremium;
            contract.TotalPayments = totalPayments;
            contract.TotalWithdrawals = totalWithdrawals;
            contract.NetInvested = netInvested;

            contract.CurrentValue = Math.Round(totalContractValue, 2);

            contract.PerformancePercent = netInvested > 0
                ? Math.Round((contract.CurrentValue - netInvested) / netInvested * 100m, 4, MidpointRounding.AwayFromZero)
                : 0m;

            contract.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();


            // 🔥 CONSOLIDATION PAR SUPPORT (DEBUG / ANALYSE)
            var grouped = holdings
                .GroupBy(h => h.SupportId)
                .Select(g => new
                {
                    SupportId = g.Key,
                    TotalInvested = g.Sum(x => x.TotalInvested),
                    TotalValue = g.Sum(x => x.CurrentAmount ?? 0m)
                });

            foreach (var g in grouped)
            {
                var perf = g.TotalInvested > 0
                    ? (g.TotalValue - g.TotalInvested) / g.TotalInvested * 100m
                    : 0m;

                _logger.LogWarning(
                    "🔎 Support GLOBAL → Support={SupportId} Value={Value} Invested={Invested} Perf={Perf}",
                    g.SupportId,
                    g.TotalValue,
                    g.TotalInvested,
                    Math.Round(perf, 4)
                );
            }

            // 🔟 Suppression holdings vides
            var emptyHoldings = holdings.Where(h => h.TotalShares <= 0).ToList();
            if (emptyHoldings.Any())
            {
                _context.ContractSupportHoldings.RemoveRange(emptyHoldings);
                await _context.SaveChangesAsync();
            }

            // 1️⃣1️⃣ Commit
            await transaction.CommitAsync();
            _context.ChangeTracker.Clear();

            return contract.CurrentValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erreur ComputeContractValueAsync pour contrat {ContractId}", contractId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ==========================================================
    // 🔁 UpdateFsaAmountsForSupportAsync (gardé intact)
    // ==========================================================
    public async Task<int> UpdateFsaAmountsForSupportAsync(int supportId)
    {
        _logger.LogInformation("🔁 Début UpdateFsaAmountsForSupportAsync pour supportId={SupportId}", supportId);

        var fsas = await _context.FinancialSupportAllocations
            .Include(f => f.Support)
            .Where(f => f.SupportId == supportId)
            .ToListAsync();

        if (!fsas.Any())
            return 0;

        decimal vl = fsas.First().Support?.LastValuationAmount ?? 0m;

        foreach (var fsa in fsas)
        {
            fsa.CurrentAmount = Math.Round(fsa.CurrentShares * vl, 7);
            fsa.UpdatedDate = DateTime.UtcNow;

            // InvestedAmount NE CHANGE PAS
        }

        await _context.SaveChangesAsync();
        return fsas.Count;
    }
}
