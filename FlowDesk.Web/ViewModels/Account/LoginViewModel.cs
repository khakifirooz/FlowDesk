using System.ComponentModel.DataAnnotations;

namespace FlowDesk.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "ایمیل را وارد کنید.")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر وارد کنید.")]
    [StringLength(256, ErrorMessage = "ایمیل بیش از حد طولانی است.")]
    [Display(Name = "ایمیل")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور را وارد کنید.")]
    [StringLength(128, ErrorMessage = "رمز عبور بیش از حد طولانی است.")]
    [DataType(DataType.Password)]
    [Display(Name = "رمز عبور")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "مرا به خاطر بسپار")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}