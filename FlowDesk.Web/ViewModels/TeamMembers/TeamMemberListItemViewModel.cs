namespace FlowDesk.Web.ViewModels.TeamMembers;

public class TeamMemberListItemViewModel
{
    public int Id { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public string FullName { get; set; } = string.Empty;
}