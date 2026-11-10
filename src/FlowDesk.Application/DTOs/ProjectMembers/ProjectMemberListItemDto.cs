namespace FlowDesk.Application.DTOs.ProjectMembers;

public class ProjectMemberListItemDto
{
    public int Id { get; set; }
    public int TeamMemberId { get; set; }
    public string FullName { get; set; } = string.Empty;
}