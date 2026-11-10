using System.ComponentModel.DataAnnotations;

namespace FlowDesk.Web.ViewModels.TeamMembers;

public class CreateTeamMemberViewModel
{
    [Required(ErrorMessage = "نام عضو را وارد کنید.")]
    [Display(Name = "نام و نام خانوادگی")]
    [StringLength(200, ErrorMessage = "نام حداکثر می‌تواند ۲۰۰ کاراکتر باشد.")]
    public string FullName { get; set; } = string.Empty;
}