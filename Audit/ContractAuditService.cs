using api.Data;
using Microsoft.EntityFrameworkCore;

namespace api.Interfaces
{
    public class ContractAuditService : IContractAuditService
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<ContractAuditService> _logger;

        public ContractAuditService(
            ApplicationDBContext context,
            ILogger<ContractAuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogContractIntegrityAsync(int contractId)
        {
            var contract = await _context.Contracts
                .Include(c => c.Compartments)
                    .ThenInclude(comp => comp.Supports)
                        .ThenInclude(s => s.Support)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                _logger.LogWarning("❌ Audit : contrat {ContractId} introuvable.", contractId);
                return;
            }

            decimal totalFromCompartments = contract.Compartments
                .Sum(c => c.Supports.Sum(s =>
                    s.CurrentShares * (s.Support?.LastValuationAmount ?? 0m)));

            decimal totalFromHoldings = await _context.ContractSupportHoldings
                .Where(h => h.ContractId == contractId)
                .SumAsync(h =>
                    (h.Support!.LastValuationAmount ?? 0m) * h.TotalShares);

            _logger.LogInformation(
                "📊 Audit #{ContractNumber}: Compartiments={Compartments:F2}€, Holdings={Holdings:F2}€, CurrentValue={Val:F2}€",
                contract.ContractNumber,
                totalFromCompartments,
                totalFromHoldings,
                contract.CurrentValue);

            if (Math.Abs(totalFromCompartments - totalFromHoldings) > 1m)
                _logger.LogWarning("⚠️ Incohérence détectée !");
            else
                _logger.LogInformation("✅ Cohérence confirmée.");
        }
    }

}