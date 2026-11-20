using FlowDesk.Infrastructure.Persistence.Configurations;
using FlowDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Persistence;

public class FlowDeskDbContext : DbContext
{
    public FlowDeskDbContext(DbContextOptions<FlowDeskDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects { get; set; }
    public DbSet<WorkItem> WorkItems { get; set; }
    public DbSet<WorkItemActivity> WorkItemActivities { get; set; }
    public DbSet<WorkItemReview> WorkItemReviews { get; set; }
    public DbSet<ProjectMemberComment> ProjectMemberComments { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<ProjectInvitation> ProjectInvitations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        FlowDeskModelConfiguration.Configure(modelBuilder);
    }
}
