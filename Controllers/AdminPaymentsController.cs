using api.Data;
using api.Dtos.Me;
using api.Exceptions;
using api.Interfaces;
using api.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [ApiController]
    [Route("api/admin/payments")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminPaymentsController : ControllerBase
    {
        private readonly ApplicationDBContext _db;
        private readonly IDonationPaidProcessor _paidProcessor;
        private readonly IBankAccountProtector _bankAccountProtector;
        private readonly IIbanValidator _ibanValidator;

        public AdminPaymentsController(
            ApplicationDBContext db,
            IDonationPaidProcessor paidProcessor,
            IBankAccountProtector bankAccountProtector,
            IIbanValidator ibanValidator)
        {
            _db = db;
            _paidProcessor = paidProcessor;
            _bankAccountProtector = bankAccountProtector;
            _ibanValidator = ibanValidator;
        }

        [HttpGet("organizations/{organizationId:int}/bank-accounts")]
        public async Task<IActionResult> GetOrganizationBankAccounts([FromRoute] int organizationId, CancellationToken cancellationToken)
        {
            var items = await _db.OrganizationBankAccounts
                .AsNoTracking()
                .Where(x => x.BeneficiaryOrganizationId == organizationId)
                .OrderByDescending(x => x.IsActive)
                .ThenByDescending(x => x.ValidFrom)
                .Select(x => new
                {
                    x.Id,
                    x.AccountHolder,
                    x.BankName,
                    x.CountryCode,
                    x.Currency,
                    MaskedIban = "****" + x.IbanLastFour,
                    MaskedBic = "****" + x.BicLastFour,
                    x.Instructions,
                    x.IsActive,
                    x.IsVerified,
                    x.ValidFrom,
                    x.ValidTo
                })
                .ToListAsync(cancellationToken);

            return Ok(items);
        }

        [HttpPost("organizations/{organizationId:int}/bank-accounts")]
        public async Task<IActionResult> CreateOrganizationBankAccount(
            [FromRoute] int organizationId,
            [FromBody] SaveOrganizationBankAccountDto dto,
            CancellationToken cancellationToken)
        {
            var organization = await _db.BeneficiaryOrganizations
                .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken)
                ?? throw new BusinessException("Organisme introuvable.");

            if (!_ibanValidator.TryNormalizeIban(dto.Iban, out var iban))
            {
                throw new BusinessException("IBAN invalide.");
            }

            var bic = (dto.Bic ?? string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
            if (!_ibanValidator.IsValidBic(bic))
            {
                throw new BusinessException("BIC invalide.");
            }

            if (dto.IsActive)
            {
                await _db.OrganizationBankAccounts
                    .Where(x => x.BeneficiaryOrganizationId == organizationId && x.IsActive)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.IsActive, false), cancellationToken);
                organization.IsBankTransferEnabled = true;
            }

            var account = new api.Models.OrganizationBankAccount
            {
                BeneficiaryOrganizationId = organizationId,
                AccountHolder = dto.AccountHolder.Trim(),
                BankName = string.IsNullOrWhiteSpace(dto.BankName) ? null : dto.BankName.Trim(),
                CountryCode = string.IsNullOrWhiteSpace(dto.CountryCode) ? iban[..2] : dto.CountryCode.Trim().ToUpperInvariant(),
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "EUR" : dto.Currency.Trim().ToUpperInvariant(),
                EncryptedIban = _bankAccountProtector.Protect(iban),
                IbanLastFour = iban[^4..],
                EncryptedBic = _bankAccountProtector.Protect(bic),
                BicLastFour = bic.Length >= 4 ? bic[^4..] : bic,
                Instructions = string.IsNullOrWhiteSpace(dto.Instructions) ? null : dto.Instructions.Trim(),
                IsActive = dto.IsActive,
                IsVerified = dto.IsVerified,
                ValidFrom = dto.ValidFrom ?? DateTime.UtcNow,
                ValidTo = dto.ValidTo,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.OrganizationBankAccounts.Add(account);
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                account.Id,
                account.AccountHolder,
                account.BankName,
                account.CountryCode,
                account.Currency,
                MaskedIban = "****" + account.IbanLastFour,
                MaskedBic = "****" + account.BicLastFour,
                account.IsActive,
                account.IsVerified
            });
        }

        [HttpGet("bank-transfers/pending")]
        public async Task<IActionResult> GetPendingBankTransfers(CancellationToken cancellationToken)
        {
            var items = await _db.PaymentAttempts
                .Include(x => x.Donation)
                    .ThenInclude(x => x.DonorSnapshot)
                .Include(x => x.Donation)
                    .ThenInclude(x => x.Donor)
                .Include(x => x.Donation)
                    .ThenInclude(x => x.Organization)
                .AsNoTracking()
                .Where(x => x.Provider == PaymentProvider.BankTransfer
                    && x.PaymentStatus != PaymentStatus.Succeeded
                    && x.PaymentStatus != PaymentStatus.Failed
                    && x.PaymentStatus != PaymentStatus.Cancelled)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.InternalReference,
                    x.PaymentStatus,
                    x.Amount,
                    x.Currency,
                    x.CreatedAt,
                    x.DonorTransferDeclaredAt,
                    x.DonorTransferDeclarationComment,
                    DonationPublicId = x.Donation.PublicId,
                    x.Donation.Reference,
                    OrganizationName = x.Donation.Organization.Name,
                    DonorName = (x.Donation.DonorSnapshot != null
                        ? x.Donation.DonorSnapshot.FirstName + " " + x.Donation.DonorSnapshot.LastName
                        : x.Donation.Donor.FirstName + " " + x.Donation.Donor.LastName)
                })
                .ToListAsync(cancellationToken);

            return Ok(items);
        }

        [HttpPost("bank-transfers/{attemptId:int}/confirm")]
        public async Task<IActionResult> ConfirmBankTransfer([FromRoute] int attemptId, [FromBody] AdminBankTransferDecisionDto dto, CancellationToken cancellationToken)
        {
            var attempt = await LoadBankAttemptAsync(attemptId, cancellationToken);
            if (attempt.PaymentStatus == PaymentStatus.Succeeded)
            {
                return Ok(new { message = "Virement deja confirme." });
            }

            var now = DateTime.UtcNow;
            attempt.PaymentStatus = PaymentStatus.Succeeded;
            attempt.PaidAt ??= now;
            attempt.ConfirmedAt ??= now;
            attempt.AdminNote = dto.Note;
            attempt.UpdatedAt = now;
            attempt.Donation.PaymentConfirmedAt ??= now;
            attempt.Donation.ConfirmedPaymentProvider = PaymentProvider.BankTransfer;
            attempt.Donation.PaymentMethod = DonationPaymentMethod.BankTransfer;
            attempt.Donation.Status = DonationStatus.Paid;
            attempt.Donation.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);

            await _paidProcessor.ProcessAsync(attempt.DonationId, User.Identity?.Name ?? "admin-bank-transfer", cancellationToken);
            return Ok(new { message = "Virement confirme et traitement post-paiement lance." });
        }

        [HttpPost("bank-transfers/{attemptId:int}/reject")]
        public async Task<IActionResult> RejectBankTransfer([FromRoute] int attemptId, [FromBody] AdminBankTransferDecisionDto dto, CancellationToken cancellationToken)
        {
            var attempt = await LoadBankAttemptAsync(attemptId, cancellationToken);
            attempt.PaymentStatus = PaymentStatus.Failed;
            attempt.FailedAt = DateTime.UtcNow;
            attempt.FailureMessage = string.IsNullOrWhiteSpace(dto.Note) ? "Virement refuse par un administrateur." : dto.Note.Trim();
            attempt.AdminNote = dto.Note;
            attempt.UpdatedAt = DateTime.UtcNow;
            attempt.Donation.Status = DonationStatus.Failed;
            attempt.Donation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Virement refuse." });
        }

        private async Task<api.Models.PaymentAttempt> LoadBankAttemptAsync(int attemptId, CancellationToken cancellationToken)
        {
            return await _db.PaymentAttempts
                .Include(x => x.Donation)
                .FirstOrDefaultAsync(x => x.Id == attemptId && x.Provider == PaymentProvider.BankTransfer, cancellationToken)
                ?? throw new BusinessException("Tentative de virement introuvable.");
        }
    }

    public sealed record AdminBankTransferDecisionDto(string? Note);

    public sealed record SaveOrganizationBankAccountDto(
        string AccountHolder,
        string Iban,
        string Bic,
        string? BankName,
        string? CountryCode,
        string? Currency,
        string? Instructions,
        bool IsActive,
        bool IsVerified,
        DateTime? ValidFrom,
        DateTime? ValidTo);
}
