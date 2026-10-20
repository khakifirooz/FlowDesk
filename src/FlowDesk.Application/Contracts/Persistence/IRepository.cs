using FlowDesk.Domain.Common;

namespace FlowDesk.Application.Contracts.Persistence;

public interface IRepository<TEntity>
    where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(int id);

    Task<List<TEntity>> GetAllAsync();

    Task AddAsync(TEntity entity);

    Task<bool> ExistsAsync(int id);
}