using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FlowDesk.Web.ViewModels.WorkItems;

public class CreateWorkItemViewModel
{
    [Required(ErrorMessage = "عنوان تسک را وارد کنید.")]
    [Display(Name = "عنوان تسک")]
    public string Title { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "یک پروژه انتخاب کنید.")]
    [Display(Name = "پروژه")]
    public int ProjectId { get; set; }

    [BindNever]
    [ValidateNever]
    public List<SelectListItem> Projects { get; set; } = new();
}