using System.ComponentModel.DataAnnotations;

namespace FlowDesk.Web.ViewModels.Account;

public sealed class ProfileViewModel
{
    [Required(ErrorMessage = "نام نمایشی را وارد کنید.")]
    [StringLength(200, ErrorMessage = "نام نمایشی حداکثر ۲۰۰ کاراکتر است.")]
    [Display(Name = "نام نمایشی")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "نام کاربری را وارد کنید.")]
    [StringLength(256, MinimumLength = 3, ErrorMessage = "نام کاربری باید بین ۳ و ۲۵۶ کاراکتر باشد.")]
    [RegularExpression("^[A-Za-z0-9_.+@-]+$", ErrorMessage = "برای نام کاربری از حروف انگلیسی، عدد یا نشانه‌های مجاز استفاده کنید.")]
    [Display(Name = "نام کاربری")]
    public string UserName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "عنوان شغلی حداکثر ۱۰۰ کاراکتر است.")]
    [Display(Name = "عنوان شغلی")]
    public string? JobTitle { get; set; }

    [StringLength(400, ErrorMessage = "معرفی کوتاه حداکثر ۴۰۰ کاراکتر است.")]
    [Display(Name = "معرفی کوتاه")]
    public string? Bio { get; set; }
}
