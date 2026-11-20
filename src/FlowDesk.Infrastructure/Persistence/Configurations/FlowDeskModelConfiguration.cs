using FlowDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Persistence.Configurations;

internal static class FlowDeskModelConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>()
            .Property(project => project.Name)
            .HasMaxLength(200)
            .UseCollation("Latin1_General_100_CI_AS")
            .IsRequired();

        modelBuilder.Entity<Project>()
            .HasIndex(project => project.Name)
            .IsUnique();

        var workItem = modelBuilder.Entity<WorkItem>();
        workItem.Property(x => x.Description).HasMaxLength(2000);
        workItem.Property(x => x.Revision).IsConcurrencyToken();
        workItem.HasIndex(x => new { x.ProjectId, x.ArchivedAtUtc });

        var activity = modelBuilder.Entity<WorkItemActivity>();
        activity.Property(x => x.ActorUserId).HasMaxLength(450).IsRequired();
        activity.Property(x => x.AuthorName).HasMaxLength(200);
        activity.Property(x => x.Message).HasMaxLength(WorkItemActivity.MessageMaxLength);
        activity.HasIndex(x => new { x.WorkItemId, x.Id });
        activity.HasOne(x => x.WorkItem).WithMany().HasForeignKey(x => x.WorkItemId)
            .OnDelete(DeleteBehavior.Restrict);

        workItem.Property(item => item.Title)
            .HasMaxLength(200)
            .UseCollation("Latin1_General_100_CI_AS")
            .IsRequired();

        modelBuilder.Entity<WorkItem>()
            .HasIndex(item => new { item.ProjectId, item.Title })
            .IsUnique();

        modelBuilder.Entity<WorkItem>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(item => item.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TeamMember>()
            .Property(member => member.FullName)
            .HasMaxLength(200)
            .UseCollation("Latin1_General_100_CI_AS")
            .IsRequired();

        modelBuilder.Entity<TeamMember>()
            .HasIndex(member => member.FullName)
            .IsUnique();

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
            .HasIndex(member => new { member.ProjectId, member.TeamMemberId })
            .IsUnique();

        modelBuilder.Entity<ProjectMember>()
            .Property(member => member.AccountUserId)
            .HasMaxLength(450)
            .IsConcurrencyToken();

        modelBuilder.Entity<ProjectMember>()
            .HasIndex(member => new { member.ProjectId, member.AccountUserId })
            .IsUnique()
            .HasFilter("[AccountUserId] IS NOT NULL");

        modelBuilder.Entity<ProjectMember>()
            .HasAlternateKey(member => new { member.ProjectId, member.Id });

        var invitation = modelBuilder.Entity<ProjectInvitation>();
        invitation.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        invitation.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
        invitation.Property(x => x.Version).IsConcurrencyToken();
        invitation.HasIndex(x => x.MembershipId).IsUnique();
        invitation.HasIndex(x => x.TokenHash).IsUnique();
        invitation.HasOne<ProjectMember>().WithMany()
            .HasForeignKey(x => new { x.ProjectId, x.MembershipId })
            .HasPrincipalKey(member => new { member.ProjectId, member.Id })
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkItem>()
            .HasOne<ProjectMember>()
            .WithMany()
            .HasForeignKey(item => new { item.ProjectId, item.AssignedProjectMemberId })
            .HasPrincipalKey(member => new { member.ProjectId, member.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
