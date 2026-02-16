using System.Collections.Generic;
using System.Threading.Tasks;
using api.Dtos.Generic;
using api.Models;
using api.Helpers;

namespace api.Interfaces
{
    public interface IEntityHistoryRepository
    {
        Task SaveEntityHistoryAsync(EntityHistory history);
        Task<IEnumerable<EntityHistory>> GetHistoryForEntityAsync(string EntityName, int entityId);

    }
}
