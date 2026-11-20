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
        string managerUserId, int? projectId, DateOnly today, CancellationToken cancellationToken)
    {
        var managed = db.Projects.AsNoTracking().Where(p => p.ManagerUserId == managerUserId);
        var projects = await managed.OrderBy(p => p.Name).ThenBy(p => p.Id)
            .Select(p => new ManagedProjectOption(p.Id, p.Name)).ToListAsync(cancellationToken);

        if (projectId is int selectedId && !projects.Any(p => p.Id == selectedId))
            return null;

        var visible = projects.Where(p => !projectId.HasValue || p.Id == projectId.Value);
        if (projects.Count == 0)
            return new(today, projectId, projects, [], new(0, 0, 0, 0, 0), [], [], []);

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

        return new(today, projectId, projects, progress, counts, deadlines, workload, activity);
    }
}
