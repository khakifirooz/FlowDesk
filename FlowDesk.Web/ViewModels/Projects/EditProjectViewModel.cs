using System.ComponentModel.DataAnnotations;

namespace FlowDesk.Web.ViewModels.Projects;

public class EditProjectViewModel
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "شناسهٔ پروژه معتبر نیست.")]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام پروژه را وارد کنید.")]
    [StringLength(
        200,
        ErrorMessage = "نام پروژه حداکثر ۲۰۰ کاراکتر است.")]
    [Display(Name = "نام پروژه")]
    public string Name { get; set; } = string.Empty;
}