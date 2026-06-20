using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Models;
using api.Services;

public class OperationRepository : IOperationRepository
{
    private readonly ApplicationDBContext _context;
    private readonly IContractValuationService _valuationService;
    private readonly BusinessRuleValidator _validator;
    private readonly IAdvanceOperationService _advanceOperationService;
    private readonly ILogger<OperationRepository> _logger;

    public OperationRepository(
        ApplicationDBContext context,
        IContractValuationService valuationService,
        BusinessRuleValidator validator,
        IAdvanceOperationService advanceOperationService,
        ILogger<OperationRepository> logger)
    {
        _context = context;
        _valuationService = valuationService;
        _validator = validator;
        _advanceOperationService = advanceOperationService;
        _logger = logger;
    }

    public async Task<PagedResult<Operation>> GetAllAsync(QueryObject query)
    {
        var operations = _context.Operations
            .Include(o => o.WithdrawalDetail)
            .Include(o => o.ArbitrageDetail)
            .Include(o => o.AdvanceDetail).ThenInclude(d => d!.Advance)
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
            .Include(o => o.AdvanceDetail).ThenInclude(d => d!.Advance)
            .Include(o => o.PaymentDetail)
            .Include(o => o.Allocations).ThenInclude(a => a.Support)
            .Include(o => o.Contract)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IEnumerable<Operation>> GetByContractAsync(int contractId) =>
        await _context.Operations
            .Where(o => o.ContractId == contractId)
            .Include(o => o.WithdrawalDetail)
            .Include(o => o.ArbitrageDetail)
            .Include(o => o.AdvanceDetail).ThenInclude(d => d!.Advance)
            .Include(o => o.PaymentDetail)
            .Include(o => o.Allocations).ThenInclude(a => a.Support)
            .Include(o => o.Contract)
            .ToListAsync();

    public async Task<Operation> AddAsync(Operation operation)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            if (operation.Type == OperationType.ScheduledPayment && operation.PaymentDetail != null)
            {
                operation.PaymentDetail.ScheduleStatus ??= OperationScheduleStatus.Active;
                operation.PaymentDetail.ScheduleGroupId ??= Guid.NewGuid().ToString("N");
                operation.PaymentDetail.StoppedAt = null;
            }

            ValidateScheduledPaymentDefinition(operation);
            await _advanceOperationService.ValidateForCreationAsync(operation);

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
            var normalizedAllocations = NormalizeAllocations(operation.Type, rawAllocations);
            var cleanAllocations = new List<OperationSupportAllocation>();

            foreach (var a in normalizedAllocations)
            {
                if (a.CompartmentId == null || a.CompartmentId <= 0)
                {
                    throw new InvalidOperationException(
                        $"CompartmentId obligatoire dans les allocations (support {a.SupportId})"
                    );
                }

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
                    CompartmentId = a.CompartmentId,

                    Amount = a.Amount,
                    Percentage = a.Percentage,

                    Flow = a.Flow,

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
            .Include(o => o.PaymentDetail)
            .Include(o => o.WithdrawalDetail)
            .Include(o => o.ArbitrageDetail)
            .Include(o => o.AdvanceDetail)
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

        await _advanceOperationService.ValidateForCreationAsync(operation);

        // ==========================================================
        // 1️⃣ Mise à jour des champs simples (sans impact financier)
        // ==========================================================
        existing.Type = operation.Type;
        existing.OperationDate = operation.OperationDate;
        existing.Amount = operation.Amount;
        existing.Currency = operation.Currency;
        existing.UpdatedDate = DateTime.UtcNow;

        if (operation.PaymentDetail != null)
        {
            if (existing.PaymentDetail == null)
            {
                existing.PaymentDetail = new PaymentDetail
                {
                    OperationId = existing.Id,
                };
            }

            existing.PaymentDetail.PaymentMethod = operation.PaymentDetail.PaymentMethod;
            existing.PaymentDetail.SourceOfFunds = operation.PaymentDetail.SourceOfFunds;
            existing.PaymentDetail.Frequency = operation.PaymentDetail.Frequency;
            existing.PaymentDetail.StartDate = operation.PaymentDetail.StartDate;
            existing.PaymentDetail.ScheduleStatus = operation.PaymentDetail.ScheduleStatus;
            existing.PaymentDetail.ScheduleGroupId = operation.PaymentDetail.ScheduleGroupId ?? existing.PaymentDetail.ScheduleGroupId;
            existing.PaymentDetail.SuspendedAt = operation.PaymentDetail.SuspendedAt;
            existing.PaymentDetail.StoppedAt = operation.PaymentDetail.StoppedAt;
        }

        if (operation.WithdrawalDetail != null)
        {
            if (existing.WithdrawalDetail == null)
            {
                existing.WithdrawalDetail = new WithdrawalDetail
                {
                    OperationId = existing.Id,
                };
            }

            existing.WithdrawalDetail.GrossAmount = operation.WithdrawalDetail.GrossAmount;
            existing.WithdrawalDetail.IsScheduled = operation.WithdrawalDetail.IsScheduled;
            existing.WithdrawalDetail.Frequency = operation.WithdrawalDetail.Frequency;
            existing.WithdrawalDetail.ScheduleGroupId =
                operation.WithdrawalDetail.ScheduleGroupId ?? existing.WithdrawalDetail.ScheduleGroupId;
        }

        if (operation.ArbitrageDetail != null)
        {
            if (existing.ArbitrageDetail == null)
            {
                existing.ArbitrageDetail = new ArbitrageDetail
                {
                    OperationId = existing.Id,
                };
            }

            existing.ArbitrageDetail.FromSupportId = operation.ArbitrageDetail.FromSupportId;
            existing.ArbitrageDetail.ToSupportId = operation.ArbitrageDetail.ToSupportId;
            existing.ArbitrageDetail.Percentage = operation.ArbitrageDetail.Percentage;
            existing.ArbitrageDetail.ScheduleGroupId =
                operation.ArbitrageDetail.ScheduleGroupId ?? existing.ArbitrageDetail.ScheduleGroupId;
        }

        if (operation.AdvanceDetail != null)
        {
            if (existing.AdvanceDetail == null)
            {
                existing.AdvanceDetail = new AdvanceDetail
                {
                    OperationId = existing.Id,
                };
            }

            existing.AdvanceDetail.Amount = operation.AdvanceDetail.Amount;
            existing.AdvanceDetail.InterestRate = operation.AdvanceDetail.InterestRate;
            existing.AdvanceDetail.MaturityDate = operation.AdvanceDetail.MaturityDate;
            existing.AdvanceDetail.AdvanceId = operation.AdvanceDetail.AdvanceId;
            existing.AdvanceDetail.TransactionType = operation.AdvanceDetail.TransactionType;
            existing.AdvanceDetail.Comment = operation.AdvanceDetail.Comment;
        }

        // ==========================================================
        // 2️⃣ Réécriture des allocations *uniquement en mode estimation*
        // ==========================================================
        _context.OperationSupportAllocations.RemoveRange(existing.Allocations);
        existing.Allocations.Clear();

        if (operation.Allocations != null)
        {
            var normalizedAllocations = NormalizeAllocations(existing.Type, operation.Allocations);

            foreach (var a in normalizedAllocations)
            {
                if (a.CompartmentId == null || a.CompartmentId <= 0)
                {
                    throw new InvalidOperationException(
                        $"CompartmentId obligatoire dans les allocations (support {a.SupportId})"
                    );
                }

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
                    CompartmentId = a.CompartmentId,

                    Amount = a.Amount,
                    Percentage = a.Percentage,
                    Flow = a.Flow,

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

    public async Task<Operation?> SuspendScheduleAsync(int operationId)
    {
        var operation = await _context.Operations
            .Include(o => o.PaymentDetail)
            .FirstOrDefaultAsync(o => o.Id == operationId);

        if (operation == null)
            return null;

        EnsureScheduledPayment(operation);

        var groupId = operation.PaymentDetail!.ScheduleGroupId;
        if (string.IsNullOrWhiteSpace(groupId))
            throw new InvalidOperationException("Aucun groupe de planification n'est défini pour cette opération.");

        var pendingInGroup = await _context.Operations
            .Include(o => o.PaymentDetail)
            .Where(o => o.Type == OperationType.ScheduledPayment &&
                        o.Status == OperationStatus.Pending &&
                        o.PaymentDetail != null &&
                        o.PaymentDetail.ScheduleGroupId == groupId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var op in pendingInGroup)
        {
            op.PaymentDetail!.ScheduleStatus = OperationScheduleStatus.Suspended;
            op.PaymentDetail.SuspendedAt = now;
            op.UpdatedDate = now;
        }

        await _context.SaveChangesAsync();
        return operation;
    }

    public async Task<Operation?> ResumeScheduleAsync(int operationId)
    {
        var operation = await _context.Operations
            .Include(o => o.PaymentDetail)
            .FirstOrDefaultAsync(o => o.Id == operationId);

        if (operation == null)
            return null;

        EnsureScheduledPayment(operation);

        var groupId = operation.PaymentDetail!.ScheduleGroupId;
        if (string.IsNullOrWhiteSpace(groupId))
            throw new InvalidOperationException("Aucun groupe de planification n'est défini pour cette opération.");

        var pendingInGroup = await _context.Operations
            .Include(o => o.PaymentDetail)
            .Where(o => o.Type == OperationType.ScheduledPayment &&
                        o.Status == OperationStatus.Pending &&
                        o.PaymentDetail != null &&
                        o.PaymentDetail.ScheduleGroupId == groupId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var op in pendingInGroup)
        {
            op.PaymentDetail!.ScheduleStatus = OperationScheduleStatus.Active;
            op.PaymentDetail.SuspendedAt = null;
            op.PaymentDetail.StoppedAt = null;
            op.UpdatedDate = now;
        }

        await _context.SaveChangesAsync();
        return operation;
    }

    public async Task<Operation?> StopScheduleAsync(int operationId)
    {
        var operation = await _context.Operations
            .Include(o => o.PaymentDetail)
            .FirstOrDefaultAsync(o => o.Id == operationId);

        if (operation == null)
            return null;

        EnsureScheduledPayment(operation);

        var groupId = operation.PaymentDetail!.ScheduleGroupId;
        if (string.IsNullOrWhiteSpace(groupId))
            throw new InvalidOperationException("Aucun groupe de planification n'est défini pour cette opération.");

        var pendingInGroup = await _context.Operations
            .Include(o => o.PaymentDetail)
            .Where(o => o.Type == OperationType.ScheduledPayment &&
                        o.Status == OperationStatus.Pending &&
                        o.PaymentDetail != null &&
                        o.PaymentDetail.ScheduleGroupId == groupId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var op in pendingInGroup)
        {
            op.PaymentDetail!.ScheduleStatus = OperationScheduleStatus.Stopped;
            op.PaymentDetail.StoppedAt = now;
            op.Status = OperationStatus.Cancelled;
            op.UpdatedDate = now;
        }

        await _context.SaveChangesAsync();
        return operation;
    }

    private List<OperationSupportAllocation> NormalizeAllocations(
        OperationType operationType,
        IEnumerable<OperationSupportAllocation> allocations)
    {
        var filtered = (allocations ?? Enumerable.Empty<OperationSupportAllocation>())
            .Where(a => (a.Amount ?? 0m) > 0m)
            .ToList();

        foreach (var a in filtered)
        {
            if (a.CompartmentId == null || a.CompartmentId <= 0)
            {
                throw new InvalidOperationException(
                    $"CompartmentId obligatoire dans les allocations (support {a.SupportId})");
            }

            if ((operationType == OperationType.Arbitrage || operationType == OperationType.ScheduledArbitrage) && a.Flow == null)
            {
                throw new InvalidOperationException(
                    $"Flow obligatoire pour arbitrage (support {a.SupportId})");
            }
        }

        var grouped = filtered
            .GroupBy(a => new
            {
                a.SupportId,
                a.CompartmentId,
                Flow = (operationType == OperationType.Arbitrage || operationType == OperationType.ScheduledArbitrage) ? a.Flow : null,
            })
            .Select(g => new OperationSupportAllocation
            {
                SupportId = g.Key.SupportId,
                CompartmentId = g.Key.CompartmentId,
                Flow = g.Key.Flow,
                Amount = g.Sum(x => x.Amount ?? 0m),
                Percentage = g.Any(x => x.Percentage.HasValue)
                    ? g.Sum(x => x.Percentage ?? 0m)
                    : null,
            })
            .ToList();

        if (grouped.Count != filtered.Count)
        {
            _logger.LogWarning(
                "⚠️ Allocations normalisées (fusion doublons): {BeforeCount} -> {AfterCount}",
                filtered.Count,
                grouped.Count);
        }

        return grouped;
    }

    private static void EnsureScheduledPayment(Operation operation)
    {
        if (operation.Type != OperationType.ScheduledPayment || operation.PaymentDetail == null)
        {
            throw new InvalidOperationException("Cette action est réservée aux versements programmés.");
        }
    }

    private static void ValidateScheduledPaymentDefinition(Operation operation)
    {
        if (operation.Type != OperationType.ScheduledPayment)
            return;

        if (operation.PaymentDetail == null)
            throw new InvalidOperationException("Les détails de versement programmé sont obligatoires.");

        var frequency = operation.PaymentDetail.Frequency?.ToLowerInvariant();
        if (frequency is not ("monthly" or "quarterly" or "yearly" or "manual"))
            throw new InvalidOperationException("La fréquence doit être monthly, quarterly, yearly ou manual pour un versement programmé.");

        if (!operation.PaymentDetail.StartDate.HasValue)
            throw new InvalidOperationException("La date de début est obligatoire pour un versement programmé.");
    }


}

