using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Dtos.Generic;
using api.Helpers;
using api.Models;
using api.Services;

public class OperationRepository : IOperationRepository
{
    private readonly ApplicationDBContext _context;
    private readonly IContractValuationService _valuationService;
    private readonly BusinessRuleValidator _validator;
    private readonly ILogger<OperationRepository> _logger;

    public OperationRepository(ApplicationDBContext context, IContractValuationService valuationService, BusinessRuleValidator validator, ILogger<OperationRepository> logger)
    {
        _context = context;
        _valuationService = valuationService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<PagedResult<Operation>> GetAllAsync(QueryObject query)
    {
        var operations = _context.Operations
            .Include(o => o.WithdrawalDetail)
            .Include(o => o.ArbitrageDetail)
            .Include(o => o.AdvanceDetail)
            .Include(o => o.PaymentDetail)
            .Include(o => o.Contract)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            operations = operations.Where(o =>
                o.Type.ToString().Contains(query.Search) ||
                o.Currency.Contains(query.Search));
        }

        operations = query.SortBy switch
        {
            "OperationDate" => query.IsDescending ? operations.OrderByDescending(o => o.OperationDate) : operations.OrderBy(o => o.OperationDate),
            "Amount" => query.IsDescending ? operations.OrderByDescending(o => o.Amount) : operations.OrderBy(o => o.Amount),
            _ => operations.OrderByDescending(o => o.CreatedDate)
        };

        var totalCount = await operations.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize);
        var skipNumber = (query.PageNumber - 1) * query.PageSize;

        return new PagedResult<Operation>
        {
            Items = await operations.Skip(skipNumber).Take(query.PageSize).ToListAsync(),
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = query.PageNumber < totalPages,
            CurrentPage = query.PageNumber
        };
    }

    public async Task<Operation?> GetByIdAsync(int id) =>
        await _context.Operations
            .Include(o => o.WithdrawalDetail)
            .Include(o => o.ArbitrageDetail)
            .Include(o => o.AdvanceDetail)
            .Include(o => o.PaymentDetail)
            .Include(o => o.Allocations).ThenInclude(a => a.Support)
            .Include(o => o.Contract)
            .Include(o => o.Compartment)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IEnumerable<Operation>> GetByContractAsync(int contractId) =>
        await _context.Operations
            .Where(o => o.ContractId == contractId)
            .Include(o => o.WithdrawalDetail)
            .Include(o => o.ArbitrageDetail)
            .Include(o => o.AdvanceDetail)
            .Include(o => o.PaymentDetail)
            .Include(o => o.Allocations).ThenInclude(a => a.Support)
            .Include(o => o.Contract)
            .Include(o => o.Compartment)
            .ToListAsync();

    public async Task<IEnumerable<Operation>> GetByCompartmentAsync(int contractId, int compartmentId) =>
        await _context.Operations
            .Where(o => o.ContractId == contractId && o.CompartmentId == compartmentId)
            .Include(o => o.WithdrawalDetail)
            .Include(o => o.ArbitrageDetail)
            .Include(o => o.AdvanceDetail)
            .Include(o => o.PaymentDetail)
            .Include(o => o.Allocations).ThenInclude(a => a.Support)
            .Include(o => o.Contract)
            .Include(o => o.Compartment)
            .ToListAsync();

    public async Task<Operation> AddAsync(Operation operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // ==========================================================
            // 0️⃣ Attacher automatiquement au compartiment GLOBAL (IsDefault uniquement)
            // ==========================================================
            if (operation.CompartmentId == null || operation.CompartmentId == 0)
            {
                var globalCompartmentId = await _context.Compartments
                    .Where(c => c.ContractId == operation.ContractId && c.IsDefault)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();

                if (globalCompartmentId <= 0)
                {
                    _logger.LogWarning(
                        "⚠️ Aucun compartiment Global (IsDefault) trouvé pour contrat {ContractId}",
                        operation.ContractId);
                }
                else
                {
                    operation.CompartmentId = globalCompartmentId;
                }
            }

            // ==========================================================
            // 1️⃣ Validation métier
            // ==========================================================
            _validator.Validate(operation, aggregateErrors: true);

            // ==========================================================
            // 2️⃣ Sauvegarde opération PENDING
            // ==========================================================
            var rawAllocations = operation.Allocations?.ToList() ?? new List<OperationSupportAllocation>();
            operation.Allocations = new List<OperationSupportAllocation>();
            operation.Status = OperationStatus.Pending;

            _context.Operations.Add(operation);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "🆕 Opération {OperationId} créée en Pending pour contrat {ContractId}",
                operation.Id,
                operation.ContractId);

            // ==========================================================
            // 3️⃣ Création allocations estimées
            // ==========================================================
            var cleanAllocations = new List<OperationSupportAllocation>();

            foreach (var a in rawAllocations.Where(a => (a.Amount ?? 0m) > 0m))
            {
                var support = await _context.FinancialSupports
                    .FirstOrDefaultAsync(s => s.Id == a.SupportId);

                if (support == null)
                {
                    _logger.LogWarning(
                        "⚠️ Support {SupportId} introuvable pour opération {OperationId}",
                        a.SupportId,
                        operation.Id);
                    continue;
                }

                var lastNav = support.LastValuationAmount;

                decimal? estimatedShares =
                    (a.Amount > 0m && lastNav > 0m)
                        ? Math.Round(a.Amount.Value / lastNav.Value, 7)
                        : null;

                cleanAllocations.Add(new OperationSupportAllocation
                {
                    OperationId = operation.Id,
                    SupportId = a.SupportId,
                    CompartmentId = a.CompartmentId ?? operation.CompartmentId,

                    Amount = a.Amount,
                    Percentage = a.Percentage,

                    EstimatedNav = lastNav,
                    EstimatedShares = estimatedShares,

                    Shares = null,
                    NavAtOperation = null,
                    NavDateAtOperation = null
                });
            }

            if (!cleanAllocations.Any())
            {
                _logger.LogWarning(
                    "⚠️ Opération {OperationId} sans allocations valides",
                    operation.Id);
            }

            await _context.OperationSupportAllocations.AddRangeAsync(cleanAllocations);
            await _context.SaveChangesAsync();

            operation.Allocations = cleanAllocations;

            await transaction.CommitAsync();

            return operation;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                ex,
                "❌ Erreur création opération pour contrat {ContractId}",
                operation.ContractId);

            throw;
        }
    }

    public async Task<Operation> UpdateAsync(Operation operation)
    {
        var existing = await _context.Operations
            .Include(o => o.Allocations)
            .FirstOrDefaultAsync(o => o.Id == operation.Id);

        if (existing == null)
            throw new InvalidOperationException("Operation not found.");

        // ==========================================================
        // 🚫 0️⃣ Interdiction absolue de modifier une opération exécutée
        // ==========================================================
        if (existing.Status == OperationStatus.Executed)
        {
            throw new InvalidOperationException(
                $"Impossible de modifier une opération exécutée (ID={existing.Id}). " +
                "Les parts, la VL et le PRU sont figés."
            );
        }

        // ==========================================================
        // 1️⃣ Mise à jour des champs simples (sans impact financier)
        // ==========================================================
        existing.Type = operation.Type;
        existing.OperationDate = operation.OperationDate;
        existing.Amount = operation.Amount;
        existing.Currency = operation.Currency;
        existing.CompartmentId = operation.CompartmentId;
        existing.UpdatedDate = DateTime.UtcNow;

        // ==========================================================
        // 2️⃣ Réécriture des allocations *uniquement en mode estimation*
        // ==========================================================
        _context.OperationSupportAllocations.RemoveRange(existing.Allocations);
        existing.Allocations.Clear();

        if (operation.Allocations != null)
        {
            foreach (var a in operation.Allocations.Where(a => (a.Amount ?? 0) > 0m))
            {
                var support = await _context.FinancialSupports
                    .FirstOrDefaultAsync(s => s.Id == a.SupportId);

                var lastNav = support?.LastValuationAmount;

                var estimatedShares =
                    (a.Amount > 0m && lastNav > 0m)
                    ? Math.Round(a.Amount.Value / lastNav.Value, 7)
                    : (decimal?)null;

                existing.Allocations.Add(new OperationSupportAllocation
                {
                    OperationId = existing.Id,
                    SupportId = a.SupportId,
                    CompartmentId = existing.CompartmentId,

                    Amount = a.Amount,
                    Percentage = a.Percentage,

                    // ⭐ ESTIMATIONS UNIQUEMENT
                    EstimatedNav = lastNav,
                    EstimatedShares = estimatedShares,

                    // ❌ JAMAIS DE DONNÉES FINALES ICI
                    Shares = null,
                    NavAtOperation = null,
                    NavDateAtOperation = null
                });
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "📝 Opération {Id} mise à jour en Pending (estimations recalculées).",
            existing.Id);

        return existing;
    }

    public async Task DeleteAsync(int id)
    {
        var operation = await _context.Operations
            .Include(o => o.Allocations)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (operation == null)
        {
            _logger.LogWarning("⚠️ Opération {Id} introuvable pour suppression.", id);
            return;
        }

        int contractId = operation.ContractId;

        // ==========================================================
        // 🚫 1️⃣ Interdiction de supprimer une opération exécutée
        // ==========================================================
        if (operation.Status == OperationStatus.Executed)
        {
            throw new InvalidOperationException(
                $"Impossible de supprimer une opération exécutée (ID={id})."
            );
        }

        // ==========================================================
        // 2️⃣ Suppression simple (pending → aucun impact financier)
        // ==========================================================
        _context.OperationSupportAllocations.RemoveRange(operation.Allocations);
        _context.Operations.Remove(operation);

        await _context.SaveChangesAsync();

        // ==========================================================
        // 3️⃣ Recalcul contractuel (uniquement si utile)
        // ==========================================================
        await _valuationService.ComputeContractValueAsync(contractId);

        _logger.LogInformation("🗑️ Opération {Id} supprimée (Pending). Contrat {ContractId} recalculé.",
            id, contractId);

        if (!operation.Allocations.Any())
        {
            _logger.LogWarning(
                "⚠️ Suppression opération {OperationId} sans allocations détectées",
                operation.Id);
        }

    }

    public async Task<IEnumerable<Operation>> GetByContractIdAsync(int contractId)
    {
        return await _context.Operations
            .Where(o => o.ContractId == contractId)
            .ToListAsync();
    }


}

