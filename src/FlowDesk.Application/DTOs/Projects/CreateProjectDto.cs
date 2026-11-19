namespace FlowDesk.Application.DTOs.Projects;

public class CreateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public required string ManagerUserId { get; init; }
}