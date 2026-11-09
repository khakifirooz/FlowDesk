namespace FlowDesk.Application.DTOs.WorkItems;

public class CreateWorkItemDto
{
    public string Title { get; set; } = string.Empty;
    public int ProjectId { get; set; }
}