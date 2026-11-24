using System.ComponentModel.DataAnnotations;

namespace FlowDesk.Web.ViewModels.Account;

public sealed class DeleteAccountViewModel
{
    [Required(ErrorMessage = "رمز عبور فعلی را وارد کنید.")]
    [DataType(DataType.Password)]
    [Display(Name = "رمز عبور فعلی")]
    public string Password { get; set; } = string.Empty;

    public bool ConfirmDeletion { get; set; }
}
