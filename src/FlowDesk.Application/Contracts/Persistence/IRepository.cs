using FlowDesk.Domain.Common;
using System.Linq.Expressions;

namespace FlowDesk.Application.Contracts.Persistence;

public interface IRepository<TEntity>
    where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(int id);

    Task<List<TEntity>> GetAllAsync();

    Task AddAsync(TEntity entity);

    Task<bool> ExistsAsync(int id);
    Task<List<TEntity>> FindAsync(
    Expression<Func<TEntity, bool>> predicate);
} 