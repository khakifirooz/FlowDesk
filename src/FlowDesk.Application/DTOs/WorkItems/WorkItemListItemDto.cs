using FlowDesk.Domain.Enums;

namespace FlowDesk.Application.DTOs.WorkItems;

public class WorkItemListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public WorkItemStatus Status { get; set; }
    public int? AssignedProjectMemberId { get; set; }
}