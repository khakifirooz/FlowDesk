using FlowDesk.Application.Contracts.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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
        try
        {
            return await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InvalidOperationException(
                "این اطلاعات هم‌زمان تغییر کرده است. صفحه را تازه کنید و دوباره تلاش کنید.",
                exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sql &&
                  (sql.Number == 2601 || sql.Number == 2627))
        {
            throw new InvalidOperationException(
                "اطلاعات تکراری است و قبلاً ثبت شده است.",
                exception);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sql && sql.Number == 547)
        {
            throw new InvalidOperationException(
                "عملیات به دلیل ارتباط این اطلاعات با رکوردهای دیگر مجاز نیست.",
                exception);
        }
    }
}