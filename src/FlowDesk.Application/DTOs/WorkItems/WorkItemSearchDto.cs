using FlowDesk.Domain.Enums;

namespace FlowDesk.Application.DTOs.WorkItems;

public class WorkItemSearchDto
{
    public string? SearchText { get; set; }
    public int? ProjectId { get; set; }
    public int PageNumber { get; set; } = 1;
    public WorkItemStatus? Status { get; set; }
}