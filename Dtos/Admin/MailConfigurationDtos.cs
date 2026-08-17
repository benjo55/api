namespace api.Dtos.Admin
{
    public sealed record AdminMailConfigurationDto(
        string Provider,
        string? Host,
        int Port,
        bool EnableSsl,
        string FromAddress,
        string FromName,
        string? UserNameHint,
        bool HasUserName,
        bool HasPassword,
        bool IsConfigured,
        string[] MissingSettings);

    public sealed record AdminMailTestRequestDto(string Recipient);

    public sealed record AdminMailTestResponseDto(
        bool Sent,
        string Recipient,
        string MessageId,
        DateTime TimestampUtc,
        string Message);
}
