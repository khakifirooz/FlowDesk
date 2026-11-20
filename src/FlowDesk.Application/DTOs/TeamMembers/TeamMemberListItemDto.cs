namespace FlowDesk.Application.DTOs.TeamMembers;

public class TeamMemberListItemDto
{
    public int Id { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public string FullName { get; set; } = string.Empty;
}