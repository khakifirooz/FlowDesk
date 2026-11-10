using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Domain.Common;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowDesk.Infrastructure.Repositories;

public class Repository<TEntity> : IRepository<TEntity>
    where TEntity : BaseEntity
{
    private readonly FlowDeskDbContext _context;

    public Repository(FlowDeskDbContext context)
    {
        _context = context;
    }

    public async Task<TEntity?> GetByIdAsync(int id)
    {
        return await _context.Set<TEntity>().FindAsync(id);
    }

    public async Task<List<TEntity>> GetAllAsync()
    {
        return await _context.Set<TEntity>().ToListAsync();
    }

    public async Task AddAsync(TEntity entity)
    {
        await _context.Set<TEntity>().AddAsync(entity);
    }

    public async Task<bool> ExistsAsync(int id)
    {
       return await _context.Set<TEntity>().AnyAsync(x => x.Id == id);
    }

    public async Task<List<TEntity>> FindAsync(
    Expression<Func<TEntity, bool>> predicate)
    {
        return await _context.Set<TEntity>()
            .Where(predicate)
            .ToListAsync();
    }

    public async Task<int> CountAsync(
    Expression<Func<TEntity, bool>> predicate)
    {
        return await _context.Set<TEntity>()
            .CountAsync(predicate);
    }

    public async Task<List<TEntity>> GetPageAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageNumber,
        int pageSize)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber));

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentOutOfRangeException(nameof(pageSize));

        return await _context.Set<TEntity>()
            .AsNoTracking()
            .Where(predicate)
            .OrderByDescending(entity => entity.Id)
            .Skip(checked((pageNumber - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync();
    }

    public void Remove(TEntity entity)
    {
        _context.Set<TEntity>().Remove(entity);
    }

}