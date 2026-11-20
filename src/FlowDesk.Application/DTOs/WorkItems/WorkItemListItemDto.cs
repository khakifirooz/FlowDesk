using FlowDesk.Domain.Enums;

namespace FlowDesk.Application.DTOs.WorkItems;

public class WorkItemListItemDto
{
    public bool CanManage { get; set; }
    public bool CanChangeStatus { get; set; }
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public WorkItemStatus Status { get; set; }
    public int? AssignedProjectMemberId { get; set; }
    public string? Description { get; set; }
    public WorkItemPriority Priority { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset? CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? ArchivedAtUtc { get; set; }
}