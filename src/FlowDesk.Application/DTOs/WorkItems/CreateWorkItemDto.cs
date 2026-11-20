using FlowDesk.Domain.Enums;

namespace FlowDesk.Application.DTOs.WorkItems;

public class CreateWorkItemDto
{
    public string Title { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public int AssignedProjectMemberId { get; set; }
    public string? Description { get; set; }
    public WorkItemPriority Priority { get; set; }
    public DateOnly? DueDate { get; set; }
}