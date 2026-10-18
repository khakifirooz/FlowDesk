using System.Threading.Tasks;

namespace FlowDesk.Application.Contracts.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
}