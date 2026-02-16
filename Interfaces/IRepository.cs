using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Generic;
using api.Dtos.Brand;
using api.Helpers;
using api.Models;

namespace api.Interfaces;
public interface IRepository<TEntity> where TEntity : class
{
    Task<PagedResult<TEntity>> GetAllAsync(QueryObject query);
    Task<TEntity?> GetByIdAsync(int id);
    Task<TEntity> CreateAsync(TEntity entity);
    Task<TEntity?> UpdateAsync(int id, object updateDto);
    Task<TEntity?> DeleteAsync(int id);
}
