using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Person;
using api.Dtos.Generic;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace api.Repository;

public abstract class RepositoryBase<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly ApplicationDBContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    protected RepositoryBase(ApplicationDBContext context)
    {
        _context = context;
        _dbSet = _context.Set<TEntity>();
    }

    public virtual async Task<PagedResult<TEntity>> GetAllAsync(QueryObject query)
    {
        var entities = _dbSet.AsQueryable();
        var totalCount = await entities.CountAsync();
        var pagedEntities = await entities
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<TEntity>
        {
            Items = pagedEntities,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / query.PageSize),
            HasNextPage = query.PageNumber * query.PageSize < totalCount,
            CurrentPage = query.PageNumber
        };
    }

    public virtual async Task<TEntity?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

    public virtual async Task<TEntity> CreateAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public abstract Task<TEntity?> UpdateAsync(int id, object updateDto);


    public virtual async Task<TEntity?> DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null) return null;
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
}
