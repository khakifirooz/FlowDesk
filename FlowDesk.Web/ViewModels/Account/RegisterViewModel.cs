using System.ComponentModel.DataAnnotations;

namespace FlowDesk.Web.ViewModels.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "نام و نام خانوادگی را وارد کنید.")]
    [StringLength(200, ErrorMessage = "نام حداکثر ۲۰۰ کاراکتر است.")]
    [Display(Name = "نام و نام خانوادگی")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "ایمیل را وارد کنید.")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر وارد کنید.")]
    [StringLength(256, ErrorMessage = "ایمیل بیش از حد طولانی است.")]
    [Display(Name = "ایمیل")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز عبور را وارد کنید.")]
    [StringLength(
        128,
        MinimumLength = 10,
        ErrorMessage = "رمز عبور باید بین ۱۰ تا ۱۲۸ کاراکتر باشد.")]
    [DataType(DataType.Password)]
    [Display(Name = "رمز عبور")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "تکرار رمز عبور را وارد کنید.")]
    [DataType(DataType.Password)]
    [Compare(
        nameof(Password),
        ErrorMessage = "رمز عبور و تکرار آن یکسان نیستند.")]
    [Display(Name = "تکرار رمز عبور")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}