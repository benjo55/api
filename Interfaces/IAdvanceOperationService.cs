using api.Models;

namespace api.Interfaces
{
    public interface IAdvanceOperationService
    {
        Task ValidateForCreationAsync(Operation operation);
        Task ApplyAsync(Operation operation);
    }
}
