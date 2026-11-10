using System.ComponentModel.DataAnnotations;

namespace FlowDesk.Web.ViewModels.Projects;

public class EditProjectViewModel
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required(ErrorMessage = "نام پروژه را وارد کنید.")]
    [Display(Name = "نام پروژه")]
    public string Name { get; set; } = string.Empty;
}