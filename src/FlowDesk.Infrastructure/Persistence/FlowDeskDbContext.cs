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
    public DbSet<TeamMember> TeamMembers { get; set; }

    public DbSet<ProjectMember> ProjectMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<WorkItem>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(item => item.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectMember>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(member => member.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectMember>()
            .HasOne<TeamMember>()
            .WithMany()
            .HasForeignKey(member => member.TeamMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProjectMember>()
            .HasIndex(member => new
            {
                member.ProjectId,
                member.TeamMemberId
            })
            .IsUnique();

        modelBuilder.Entity<ProjectMember>()
            .HasAlternateKey(member => new
            {
                member.ProjectId,
                member.Id
            });

        modelBuilder.Entity<WorkItem>()
            .HasOne<ProjectMember>()
            .WithMany()
            .HasForeignKey(item => new
            {
                item.ProjectId,
                item.AssignedProjectMemberId
            })
            .HasPrincipalKey(member => new
            {
                member.ProjectId,
                member.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TeamMember>()
            .Property(member => member.FullName)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<TeamMember>()
            .HasIndex(member => member.FullName)
            .IsUnique();
    }

}
