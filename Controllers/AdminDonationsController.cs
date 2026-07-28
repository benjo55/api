using api.Data;
using api.Helpers;
using api.Interfaces;
using api.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [ApiController]
    [Route("api/admin/donations")]
    [Authorize(Roles = "Admin")]
    public sealed class AdminDonationsController : ControllerBase
    {
        private readonly ApplicationDBContext _db;
        private readonly IPublicDonationService _service;

        public AdminDonationsController(ApplicationDBContext db, IPublicDonationService service)
        {
            _db = db;
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QueryObject query, CancellationToken cancellationToken)
        {
            var donations = _db.Donations
                .Include(x => x.Donor)
                .Include(x => x.PaymentAttempts)
                .Include(x => x.TaxReceipts)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<DonationStatus>(query.Status, true, out var status))
            {
                donations = donations.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                donations = donations.Where(x =>
                    (x.Reference != null && x.Reference.Contains(query.Search))
                    || x.Donor.FirstName.Contains(query.Search)
                    || x.Donor.LastName.Contains(query.Search)
                    || (x.Donor.Email != null && x.Donor.Email.Contains(query.Search)));
            }

            var totalCount = await donations.CountAsync(cancellationToken);
            var pageSize = Math.Max(1, query.PageSize);
            var pageNumber = Math.Max(1, query.PageNumber);
            var items = await donations
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.PublicId,
                    x.Reference,
                    x.CreatedAt,
                    Donor = x.Donor.FullName,
                    x.Donor.Email,
                    x.Amount,
                    x.Currency,
                    DonationStatus = x.Status.ToString(),
                    PaymentStatus = x.PaymentAttempts.OrderByDescending(p => p.CreatedAt).Select(p => p.PaymentStatus.ToString()).FirstOrDefault(),
                    CheckoutIntentId = x.PaymentAttempts.OrderByDescending(p => p.CreatedAt).Select(p => p.ProviderCheckoutIntentId).FirstOrDefault(),
                    x.PaymentConfirmedAt,
                    ReceiptNumber = x.TaxReceipts.OrderByDescending(r => r.CreatedAt).Select(r => r.ReceiptNumber).FirstOrDefault(),
                    ReceiptEmailStatus = x.TaxReceipts.OrderByDescending(r => r.CreatedAt).Select(r => r.LastEmailStatus.ToString()).FirstOrDefault(),
                })
                .ToListAsync(cancellationToken);

            return Ok(new
            {
                items,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                hasNextPage = pageNumber * pageSize < totalCount,
                currentPage = pageNumber,
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
        {
            var donation = await _db.Donations
                .Include(x => x.Donor)
                .Include(x => x.Organization)
                .Include(x => x.PaymentAttempts)
                .Include(x => x.TaxReceipts)
                .ThenInclude(x => x.EmailHistory)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            return donation is null ? NotFound() : Ok(donation);
        }

        [HttpPost("{id:int}/reconcile")]
        public async Task<IActionResult> Reconcile([FromRoute] int id, CancellationToken cancellationToken)
        {
            await _service.ForceReconcileAsync(id, cancellationToken);
            return Ok(new { message = "Reconciliation demandee." });
        }

        [HttpPost("{id:int}/resend-receipt")]
        public async Task<IActionResult> ResendReceipt([FromRoute] int id, CancellationToken cancellationToken)
        {
            await _service.ResendReceiptAsync(id, cancellationToken);
            return Ok(new { message = "Renvoi du recu lance." });
        }
    }
}
