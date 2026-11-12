using System.ComponentModel.DataAnnotations;

namespace FlowDesk.Web.ViewModels.TeamMembers;

public class EditTeamMemberViewModel : CreateTeamMemberViewModel
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "شناسهٔ عضو معتبر نیست.")]
    public int Id { get; set; }
}