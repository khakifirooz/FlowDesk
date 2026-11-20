using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.DTOs.Dashboard;
using FlowDesk.Domain.Entities;
using FlowDesk.Domain.Enums;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Infrastructure.Queries;

public sealed class ManagementDashboardReader(FlowDeskDbContext db) : IManagementDashboardReader
{
    private const int PreviewLimit = 8;

    public async Task<ManagementDashboardDto?> ReadAsync(
        string managerUserId, int? projectId, DateOnly today, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var managed = db.Projects.AsNoTracking().Where(p => p.ManagerUserId == managerUserId);
        var projects = await managed.OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Select(p => new ManagedProjectOption(p.Id, p.Name)).ToListAsync(cancellationToken);

        if (projectId is int selectedId && !projects.Any(p => p.Id == selectedId))
            return null;

        var visible = projects.Where(p => !projectId.HasValue || p.Id == projectId.Value);
        if (projects.Count == 0)
            return new(today, projectId, projects, [], new(0, 0, 0, 0, 0), [], [], [], [], [], []);

        var selectedProjects = projectId is int id ? managed.Where(p => p.Id == id) : managed;
        var items = from item in db.WorkItems.AsNoTracking()
                    join project in selectedProjects on item.ProjectId equals project.Id
                    select new { Item = item, ProjectName = project.Name };

        var active = items.Where(x => x.Item.ArchivedAtUtc == null);
        var open = active.Where(x => x.Item.Status != WorkItemStatus.Done);

        var grouped = await active.GroupBy(x => x.Item.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                Counts = new DashboardCounts(
                    g.Count(x => x.Item.Status == WorkItemStatus.ToDo),
                    g.Count(x => x.Item.Status == WorkItemStatus.InProgress),
                    g.Count(x => x.Item.Status == WorkItemStatus.Done),
                    g.Count(x => x.Item.Status != WorkItemStatus.Done && x.Item.DueDate < today),
                    g.Count(x => x.Item.Status != WorkItemStatus.Done && x.Item.AssignedProjectMemberId == null))
            }).ToDictionaryAsync(x => x.ProjectId, x => x.Counts, cancellationToken);

        var progress = visible.Select(p => new DashboardProjectSummary(
            p.Id, p.Name, grouped.GetValueOrDefault(p.Id) ?? new(0, 0, 0, 0, 0))).ToList();
        var counts = new DashboardCounts(
            progress.Sum(p => p.Counts.ToDo), progress.Sum(p => p.Counts.InProgress),
            progress.Sum(p => p.Counts.Done), progress.Sum(p => p.Counts.Overdue),
            progress.Sum(p => p.Counts.Unassigned));

        var deadlines = await open.Where(x => x.Item.DueDate.HasValue)
            .OrderBy(x => x.Item.DueDate)
            .ThenByDescending(x => x.Item.Priority)
            .ThenBy(x => x.Item.Id)
            .Take(PreviewLimit)
            .Select(x => new DashboardDeadline(
                x.Item.Id, x.Item.Title, x.ProjectName,
                (from m in db.ProjectMembers
                 join member in db.TeamMembers on m.TeamMemberId equals member.Id
                 where m.ProjectId == x.Item.ProjectId && m.Id == x.Item.AssignedProjectMemberId
                 select member.FullName).FirstOrDefault(),
                x.Item.DueDate!.Value, x.Item.Priority))
            .ToListAsync(cancellationToken);

        var workload = await (
            from x in open
            join membership in db.ProjectMembers.AsNoTracking()
                on new { x.Item.ProjectId, Id = x.Item.AssignedProjectMemberId }
                equals new { membership.ProjectId, Id = (int?)membership.Id }
            join member in db.TeamMembers.AsNoTracking() on membership.TeamMemberId equals member.Id
            group x.Item by new { member.Id, member.FullName } into g
            select new
            {
                g.Key.Id,
                g.Key.FullName,
                Open = g.Count(),
                ToDo = g.Count(x => x.Status == WorkItemStatus.ToDo),
                InProgress = g.Count(x => x.Status == WorkItemStatus.InProgress),
                Overdue = g.Count(x => x.DueDate < today)
            })
            .OrderByDescending(x => x.Open).ThenByDescending(x => x.Overdue)
            .ThenBy(x => x.FullName).ThenBy(x => x.Id).Take(PreviewLimit)
            .Select(x => new DashboardWorkload(x.FullName, x.ToDo, x.InProgress, x.Overdue))
            .ToListAsync(cancellationToken);

        var activity = await (
            from entry in db.WorkItemActivities.AsNoTracking()
            join x in items on entry.WorkItemId equals x.Item.Id
            orderby entry.Id descending
            select new DashboardActivity(x.Item.Id, x.Item.Title, x.ProjectName, entry.Kind, entry.OccurredAtUtc))
            .Take(PreviewLimit)
            .ToListAsync(cancellationToken);

        var roster = await (
            from membership in db.ProjectMembers.AsNoTracking()
            join project in selectedProjects on membership.ProjectId equals project.Id
            join member in db.TeamMembers.AsNoTracking() on membership.TeamMemberId equals member.Id
            select new { membership.Id, ProjectId = project.Id, ProjectName = project.Name, member.FullName })
            .ToListAsync(cancellationToken);

        var totalsByMembership = await active.Where(x => x.Item.AssignedProjectMemberId.HasValue)
            .GroupBy(x => x.Item.AssignedProjectMemberId!.Value)
            .Select(g => new { Id = g.Key, Total = g.Count(),
                Completed = g.Count(x => x.Item.Status == WorkItemStatus.Done) })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var latestRatings =
            from review in db.WorkItemReviews.AsNoTracking()
            join row in items on review.WorkItemId equals row.Item.Id
            where !db.WorkItemReviews.Any(newer => newer.WorkItemId == review.WorkItemId &&
                newer.CompletionActivityId > review.CompletionActivityId)
            select new { review.RecipientMembershipId, review.RecipientName, review.Score,
                row.Item.ProjectId, row.ProjectName };

        var ratingsByMembership = await latestRatings
            .GroupBy(x => new { x.RecipientMembershipId, x.ProjectId, x.ProjectName })
            .Select(g => new { Id = g.Key.RecipientMembershipId, g.Key.ProjectId, g.Key.ProjectName,
                RecipientName = g.Max(x => x.RecipientName),
                Total = g.Count(), WithScore = g.Count(x => x.Score.HasValue),
                Average = g.Average(x => (double?)x.Score) })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var currentMembershipIds = roster.Select(x => x.Id).ToHashSet();
        var reports = roster.Select(member =>
        {
            var totals = totalsByMembership.GetValueOrDefault(member.Id);
            var ratings = ratingsByMembership.GetValueOrDefault(member.Id);
            return new DashboardMemberReport(member.ProjectId, member.Id, member.ProjectName,
                member.FullName, true, totals?.Total ?? 0, totals?.Completed ?? 0,
                ratings?.Total ?? 0, ratings?.WithScore ?? 0, ratings?.Average);
        }).Concat(ratingsByMembership.Values.Where(r => !currentMembershipIds.Contains(r.Id))
            .Select(r => new DashboardMemberReport(r.ProjectId, r.Id, r.ProjectName,
                r.RecipientName ?? "عضو سابق", false, 0, 0, r.Total, r.WithScore, r.Average)))
            .OrderBy(r => r.ProjectName).ThenBy(r => r.MemberName).ThenBy(r => r.MembershipId).ToArray();

        var approvals = await (
            from invite in db.ProjectInvitations.AsNoTracking()
            join project in selectedProjects on invite.ProjectId equals project.Id
            join membership in db.ProjectMembers.AsNoTracking()
                on new { invite.ProjectId, Id = invite.MembershipId } equals new { membership.ProjectId, membership.Id }
            join member in db.TeamMembers.AsNoTracking() on membership.TeamMemberId equals member.Id
            where invite.PendingUserId != null && invite.RequestedAtUtc != null && invite.AcceptedAtUtc == null &&
                invite.RevokedAtUtc == null && membership.AccountUserId == null
            select new { project.Id, invite.MembershipId, project.Name, member.FullName,
                invite.NormalizedEmail, invite.ExpiresAtUtc, invite.RequestedAtUtc })
            .ToListAsync(cancellationToken);

        var pendingApprovals = approvals.Where(x => x.ExpiresAtUtc > now).OrderBy(x => x.RequestedAtUtc)
            .ThenBy(x => x.MembershipId).Take(PreviewLimit)
            .Select(x => new DashboardApproval(x.Id, x.MembershipId, x.Name, x.FullName, x.NormalizedEmail)).ToArray();

        var pendingReviews = await (
            from row in active
            join completion in db.WorkItemActivities.AsNoTracking() on row.Item.Id equals completion.WorkItemId
            where row.Item.Status == WorkItemStatus.Done && completion.Kind == WorkItemActivityKind.Completed &&
                !db.WorkItemActivities.Any(newer => newer.WorkItemId == row.Item.Id &&
                    newer.Id > completion.Id && newer.Kind == WorkItemActivityKind.Completed) &&
                !db.WorkItemReviews.Any(review => review.CompletionActivityId == completion.Id) &&
                (completion.CompletionRecipientCaptured
                    ? db.ProjectMembers.Any(m => m.ProjectId == row.Item.ProjectId && m.Id == completion.CompletionRecipientMembershipId)
                    : !db.WorkItemActivities.Any(newer => newer.WorkItemId == row.Item.Id &&
                        newer.Id > completion.Id && newer.Kind == WorkItemActivityKind.Reassigned) &&
                      db.ProjectMembers.Any(m => m.ProjectId == row.Item.ProjectId && m.Id == row.Item.AssignedProjectMemberId &&
                        (completion.ActorUserId == managerUserId || completion.ActorUserId == m.AccountUserId)))
            orderby completion.Id
            select new DashboardPendingReview(row.Item.Id, row.Item.Title, row.ProjectName))
            .Take(PreviewLimit).ToListAsync(cancellationToken);

        return new(today, projectId, projects, progress, counts, deadlines, workload, activity, reports, pendingApprovals, pendingReviews);
    }
}
