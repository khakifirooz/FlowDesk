using FlowDesk.Domain.Entities;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Application.DTOs.Dashboard;

public sealed record ManagedProjectOption(int Id, string Name);

public sealed record DashboardCounts(
    int ToDo, int InProgress, int Done, int Overdue, int Unassigned)
{
    public int Total => ToDo + InProgress + Done;
    public int CompletionPercent => Total == 0 ? 0 : (int)(100L * Done / Total);
}

public sealed record DashboardProjectSummary(int Id, string Name, DashboardCounts Counts);

public sealed record DashboardDeadline(
    int WorkItemId, string Title, string ProjectName, string? AssigneeName,
    DateOnly DueDate, WorkItemPriority Priority);

public sealed record DashboardWorkload(string MemberName, int ToDo, int InProgress, int Overdue)
{
    public int Open => ToDo + InProgress;
}

public sealed record DashboardActivity(
    int WorkItemId, string Title, string ProjectName,
    WorkItemActivityKind Kind, DateTimeOffset OccurredAtUtc);

public sealed record ManagementDashboardDto(
    DateOnly Today,
    int? SelectedProjectId,
    IReadOnlyList<ManagedProjectOption> Projects,
    IReadOnlyList<DashboardProjectSummary> Progress,
    DashboardCounts Counts,
    IReadOnlyList<DashboardDeadline> Deadlines,
    IReadOnlyList<DashboardWorkload> Workload,
    IReadOnlyList<DashboardActivity> Activity);
