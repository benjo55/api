namespace api.Interfaces
{
    public interface IContractAuditService
    {
        Task LogContractIntegrityAsync(int contractId);
    }
}
