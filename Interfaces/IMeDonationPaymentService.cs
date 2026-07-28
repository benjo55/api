using api.Dtos.Me;

namespace api.Interfaces
{
    public interface IMeDonationPaymentService
    {
        Task<MeDonationPaymentOptionsDto?> GetPaymentOptionsAsync(int userId, string publicId, CancellationToken cancellationToken);
        Task<MeHelloAssoPaymentStartedDto?> StartHelloAssoPaymentAsync(int userId, string publicId, CancellationToken cancellationToken);
        Task<MeBankTransferInstructionsDto?> StartBankTransferAsync(int userId, string publicId, CancellationToken cancellationToken);
        Task<MeDonationPaymentStatusDto?> GetPaymentStatusAsync(int userId, string publicId, CancellationToken cancellationToken);
        Task<MeDonationPaymentStatusDto?> DeclareBankTransferAsync(int userId, string publicId, DeclareBankTransferDto dto, CancellationToken cancellationToken);
    }

    public interface IPaymentReconciliationService
    {
        Task ReconcileHelloAssoAttemptAsync(int paymentAttemptId, CancellationToken cancellationToken);
        Task ReconcileHelloAssoCheckoutAsync(string checkoutIntentId, CancellationToken cancellationToken);
    }

    public interface IDonationPaidProcessor
    {
        Task ProcessAsync(int donationId, string actor, CancellationToken cancellationToken);
    }

    public interface IBankAccountProtector
    {
        string Protect(string value);
        string Unprotect(string protectedValue);
    }

    public interface IIbanValidator
    {
        bool TryNormalizeIban(string value, out string normalizedIban);
        bool IsValidBic(string value);
    }
}
