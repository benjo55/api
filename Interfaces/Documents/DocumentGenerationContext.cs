using System.Security.Claims;
using api.Dtos.Documents;

namespace api.Interfaces.Documents
{
    public sealed record DocumentGenerationContext(
        ClaimsPrincipal User,
        int? UserId,
        string? UserName,
        string Locale,
        string TimeZone,
        DateTimeOffset GeneratedAt,
        DocumentDeliveryMode DeliveryMode,
        string CorrelationId,
        string? DataAsOfDate);
}
