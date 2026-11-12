using System.ComponentModel.DataAnnotations;

namespace FlowDesk.Web.ViewModels.Projects;

public class CreateProjectViewModel
{
    [Required(ErrorMessage = "نام پروژه را وارد کنید.")]
    [StringLength(
        200,
        ErrorMessage = "نام پروژه حداکثر ۲۰۰ کاراکتر است.")]
    [Display(Name = "نام پروژه")]
    public string Name { get; set; } = string.Empty;
}