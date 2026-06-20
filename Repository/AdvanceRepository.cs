using api.Data;
using api.Dtos.Advance;
using api.Interfaces;
using api.Mappers;
using api.Models;
using api.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class AdvanceRepository : IAdvanceRepository
    {
        private const decimal EuroFundAdvanceRatio = 0.80m;
        private const decimal UnitLinkedAdvanceRatio = 0.60m;

        private readonly ApplicationDBContext _context;

        public AdvanceRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<List<AdvanceDto>> GetAllAsync()
        {
            var advances = await BaseQuery()
                .OrderByDescending(a => a.RequestDate)
                .ToListAsync();

            return advances.Select(a => a.ToDto()).ToList();
        }

        public async Task<List<AdvanceDto>> GetByContractIdAsync(int contractId)
        {
            var advances = await BaseQuery()
                .Where(a => a.ContractId == contractId)
                .OrderByDescending(a => a.RequestDate)
                .ToListAsync();

            return advances.Select(a => a.ToDto()).ToList();
        }

        public async Task<AdvanceDto?> GetByIdAsync(int id)
        {
            var advance = await BaseQuery().FirstOrDefaultAsync(a => a.Id == id);
            return advance?.ToDto();
        }

        public async Task<AdvanceDto> CreateAsync(CreateAdvanceRequestDto dto)
        {
            if (dto.ContractId <= 0)
                throw new InvalidOperationException("Le contrat est obligatoire.");

            if (dto.RequestedAmount <= 0m)
                throw new InvalidOperationException("Le montant demandé doit être supérieur à zéro.");

            var contractExists = await _context.Contracts.AnyAsync(c => c.Id == dto.ContractId);
            if (!contractExists)
                throw new InvalidOperationException($"Contrat {dto.ContractId} introuvable.");

            var eligibility = await GetEligibilityAsync(dto.ContractId)
                ?? throw new InvalidOperationException("Impossible de calculer l'éligibilité de l'avance.");

            if (dto.RequestedAmount > eligibility.AvailableAdvanceAmount)
            {
                throw new InvalidOperationException(
                    $"Le montant demandé dépasse le montant disponible ({eligibility.AvailableAdvanceAmount:F2}).");
            }

            var requestDate = dto.RequestDate ?? DateTime.UtcNow;
            var durationMonths = dto.DurationMonths > 0 ? dto.DurationMonths : 36;
            var advance = new Advance
            {
                ContractId = dto.ContractId,
                AdvanceNumber = string.IsNullOrWhiteSpace(dto.AdvanceNumber)
                    ? await GenerateAdvanceNumberAsync(requestDate)
                    : dto.AdvanceNumber.Trim(),
                RequestDate = requestDate,
                ApprovalDate = null,
                DisbursementDate = null,
                MaturityDate = dto.MaturityDate ?? requestDate.AddMonths(durationMonths),
                RequestedAmount = dto.RequestedAmount,
                ApprovedAmount = null,
                OutstandingCapital = 0m,
                InterestRate = dto.InterestRate,
                DurationMonths = durationMonths,
                Reason = dto.Reason,
                Status = AdvanceStatus.Requested,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            await _context.Advances.AddAsync(advance);
            await _context.SaveChangesAsync();

            return (await BaseQuery().FirstAsync(a => a.Id == advance.Id)).ToDto();
        }

        public async Task<AdvanceDto?> UpdateAsync(int id, UpdateAdvanceRequestDto dto)
        {
            var advance = await _context.Advances.FindAsync(id);
            if (advance == null)
                return null;

            if (advance.Locked)
                throw new InvalidOperationException("Cette avance est verrouillée.");

            if (dto.ApprovedAmount.HasValue && dto.ApprovedAmount.Value < 0m)
                throw new InvalidOperationException("Le montant approuvé ne peut pas être négatif.");

            if (dto.ApprovedAmount.HasValue && dto.ApprovedAmount.Value > advance.RequestedAmount)
                throw new InvalidOperationException("Le montant approuvé ne peut pas dépasser le montant demandé.");

            if (dto.Status.HasValue && dto.Status.Value != advance.Status)
            {
                var transitionAllowed =
                    advance.Status == AdvanceStatus.Requested &&
                    dto.Status.Value is AdvanceStatus.Approved or AdvanceStatus.Cancelled ||
                    advance.Status == AdvanceStatus.Approved &&
                    dto.Status.Value == AdvanceStatus.Cancelled;

                if (!transitionAllowed)
                {
                    throw new InvalidOperationException(
                        $"Transition d'avance interdite : {advance.Status} vers {dto.Status.Value}.");
                }
            }

            if (dto.Status == AdvanceStatus.Approved)
            {
                var approvedAmount = dto.ApprovedAmount ?? advance.RequestedAmount;
                if (approvedAmount <= 0m)
                    throw new InvalidOperationException("Le montant approuvé doit être supérieur à zéro.");

                var eligibility = await GetEligibilityAsync(advance.ContractId)
                    ?? throw new InvalidOperationException("Impossible de calculer l'éligibilité de l'avance.");

                if (approvedAmount > eligibility.AvailableAdvanceAmount)
                {
                    throw new InvalidOperationException(
                        $"Le montant approuvé dépasse le montant disponible ({eligibility.AvailableAdvanceAmount:F2}).");
                }

                advance.ApprovedAmount = approvedAmount;
                advance.ApprovalDate = dto.ApprovalDate ?? DateTime.UtcNow;
            }

            advance.MaturityDate = dto.MaturityDate ?? advance.MaturityDate;
            if (advance.Status == AdvanceStatus.Approved && dto.ApprovedAmount.HasValue)
                advance.ApprovedAmount = dto.ApprovedAmount;
            advance.InterestRate = dto.InterestRate ?? advance.InterestRate;
            advance.DurationMonths = dto.DurationMonths ?? advance.DurationMonths;
            advance.Reason = dto.Reason ?? advance.Reason;
            advance.Status = dto.Status ?? advance.Status;
            advance.Locked = dto.Locked ?? advance.Locked;
            advance.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return (await BaseQuery().FirstAsync(a => a.Id == id)).ToDto();
        }

        public async Task<AdvanceTransactionDto> AddTransactionAsync(int advanceId, CreateAdvanceTransactionRequestDto dto)
        {
            if (dto.Amount <= 0m)
                throw new InvalidOperationException("Le montant du mouvement doit être supérieur à zéro.");

            var advance = await _context.Advances
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.Id == advanceId)
                ?? throw new InvalidOperationException($"Avance {advanceId} introuvable.");

            if (advance.Locked)
                throw new InvalidOperationException("Cette avance est verrouillée.");

            ApplyCapitalMovement(advance, dto.Type, dto.Amount);

            var transaction = new AdvanceTransaction
            {
                AdvanceId = advance.Id,
                OperationDate = dto.OperationDate ?? DateTime.UtcNow,
                Type = dto.Type,
                Amount = dto.Amount,
                Comment = dto.Comment,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            advance.Transactions.Add(transaction);
            advance.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return transaction.ToDto();
        }

        public async Task<AdvanceEligibilityDto?> GetEligibilityAsync(int contractId)
        {
            var contractExists = await _context.Contracts.AnyAsync(c => c.Id == contractId);
            if (!contractExists)
                return null;

            var allocationRows = await _context.FinancialSupportAllocations
                .Where(a => a.ContractId == contractId)
                .Include(a => a.Support)
                .AsNoTracking()
                .ToListAsync();

            var supportValues = allocationRows
                .Select(a => new
                {
                    Support = a.Support,
                    Amount = a.CurrentAmount > 0m ? a.CurrentAmount : a.InvestedAmount
                })
                .Where(x => x.Support != null && x.Amount > 0m)
                .ToList();

            if (!supportValues.Any())
            {
                supportValues = await _context.ContractSupportHoldings
                    .Where(h => h.ContractId == contractId)
                    .Include(h => h.Support)
                    .AsNoTracking()
                    .Select(h => new
                    {
                        Support = h.Support,
                        Amount = (h.CurrentAmount ?? 0m) > 0m ? h.CurrentAmount ?? 0m : h.TotalInvested
                    })
                    .Where(x => x.Support != null && x.Amount > 0m)
                    .ToListAsync();
            }

            var euroValue = 0m;
            var unitLinkedValue = 0m;

            foreach (var row in supportValues)
            {
                var nature = ResolveNature(row.Support!);
                if (nature == FinancialSupportNature.EuroFund)
                    euroValue += row.Amount;
                else if (nature == FinancialSupportNature.UnitLinked)
                    unitLinkedValue += row.Amount;
            }

            var maximum = euroValue * EuroFundAdvanceRatio + unitLinkedValue * UnitLinkedAdvanceRatio;
            var outstanding = await _context.Advances
                .Where(a => a.ContractId == contractId &&
                            a.OutstandingCapital > 0m &&
                            a.Status != AdvanceStatus.Cancelled &&
                            a.Status != AdvanceStatus.Settled)
                .SumAsync(a => a.OutstandingCapital);

            return new AdvanceEligibilityDto
            {
                ContractId = contractId,
                EligibleEuroFundValue = euroValue,
                EligibleUnitLinkedValue = unitLinkedValue,
                MaximumAdvanceAmount = Math.Round(maximum, 2),
                OutstandingAdvanceCapital = Math.Round(outstanding, 2),
                AvailableAdvanceAmount = Math.Max(0m, Math.Round(maximum - outstanding, 2))
            };
        }

        private IQueryable<Advance> BaseQuery()
        {
            return _context.Advances
                .Include(a => a.Transactions)
                .AsNoTracking();
        }

        private async Task<string> GenerateAdvanceNumberAsync(DateTime requestDate)
        {
            var prefix = $"AV-{requestDate:yyyy}-";
            var count = await _context.Advances.CountAsync(a => a.AdvanceNumber.StartsWith(prefix));
            return $"{prefix}{count + 1:0000}";
        }

        private static void ApplyCapitalMovement(Advance advance, AdvanceTransactionType type, decimal amount)
        {
            switch (type)
            {
                case AdvanceTransactionType.Grant:
                case AdvanceTransactionType.Disbursement:
                case AdvanceTransactionType.InterestCapitalization:
                    advance.OutstandingCapital += amount;
                    advance.Status = AdvanceStatus.Active;
                    if (type == AdvanceTransactionType.Disbursement)
                        advance.DisbursementDate ??= DateTime.UtcNow;
                    break;

                case AdvanceTransactionType.PartialRepayment:
                case AdvanceTransactionType.TotalRepayment:
                    if (amount > advance.OutstandingCapital)
                        throw new InvalidOperationException("Le remboursement dépasse le capital restant dû.");

                    advance.OutstandingCapital -= amount;
                    if (type == AdvanceTransactionType.TotalRepayment || advance.OutstandingCapital <= 0m)
                    {
                        advance.OutstandingCapital = 0m;
                        advance.Status = AdvanceStatus.Settled;
                    }
                    break;

                case AdvanceTransactionType.InterestPayment:
                    break;

                case AdvanceTransactionType.Renewal:
                    advance.Status = AdvanceStatus.Renewed;
                    break;
            }
        }

        private static FinancialSupportNature? ResolveNature(FinancialSupport support)
        {
            if (support.SupportNature.HasValue)
                return support.SupportNature.Value;

            var haystack = string.Join(" ",
                    support.SupportType,
                    support.AssetClass,
                    support.SubAssetClass,
                    support.Label,
                    support.MarketingName,
                    support.LegalName)
                .ToLowerInvariant()
                .Normalize(System.Text.NormalizationForm.FormD);

            var normalized = new string(haystack
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray());

            if (normalized.Contains("fonds euro") ||
                normalized.Contains("fond euro") ||
                normalized.Contains("euro fund"))
                return FinancialSupportNature.EuroFund;

            if (normalized.Contains("unite de compte") ||
                normalized.Contains("unites de compte") ||
                normalized.Contains("unit linked") ||
                normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("uc"))
                return FinancialSupportNature.UnitLinked;

            return null;
        }
    }
}
