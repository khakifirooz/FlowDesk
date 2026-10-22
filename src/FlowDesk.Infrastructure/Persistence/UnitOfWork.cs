using FlowDesk.Application.Contracts.Persistence;

namespace FlowDesk.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly FlowDeskDbContext _context;

    public UnitOfWork(FlowDeskDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}