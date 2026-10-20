using FlowDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Persistence;

public class FlowDeskDbContext : DbContext
{
    public FlowDeskDbContext(DbContextOptions<FlowDeskDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<WorkItem> WorkItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkItem>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(workItem => workItem.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }

}
