namespace api.Interfaces
{
    public interface IHelloAssoTokenProvider
    {
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken, string? credentialKey = null);
        Task<string> RefreshAccessTokenAsync(CancellationToken cancellationToken, string? credentialKey = null);
    }
}
