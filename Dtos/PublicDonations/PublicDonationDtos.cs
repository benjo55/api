namespace api.Dtos.PublicDonations
{
    public sealed record PublicDonationDonorInput(
        string FirstName,
        string LastName,
        string Email,
        string Address,
        string PostalCode,
        string City,
        string Country);

    public sealed record PublicDonationCheckoutRequest(
        decimal Amount,
        PublicDonationDonorInput Donor);

    public sealed record PublicDonationCheckoutResponse(
        string DonationId,
        string Reference,
        string RedirectUrl);

    public sealed record PublicDonationStatusResponse(
        string Reference,
        decimal Amount,
        string Currency,
        string Status,
        bool ReceiptAvailable,
        string? ReceiptToken);

    public sealed record PublicDonationReceiptTokenResponse(string Token);
}
