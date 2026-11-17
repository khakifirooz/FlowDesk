using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Identity;

public class FlowDeskIdentityDbContext
    : IdentityDbContext<ApplicationUser>
{
    public FlowDeskIdentityDbContext(
        DbContextOptions<FlowDeskIdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .Property(user => user.FullName)
            .HasMaxLength(200)
            .IsRequired();
    }
}