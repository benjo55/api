using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Contract;
using api.Dtos.Generic;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IFieldDescriptionRepository
    {
        Task<PagedResult<FieldDescription>> GetAllAsync(QueryObject query);
        Task<FieldDescription?> GetByIdAsync(int id);
        Task<IEnumerable<FieldDescription>> GetByEntityNameAsync(string entityName);
        Task<FieldDescription> CreateAsync(FieldDescription fieldDescription);
        Task<FieldDescription?> UpdateAsync(int id, FieldDescription fieldDescription);
        Task<FieldDescription?> DeleteAsync(int id);
    }

}