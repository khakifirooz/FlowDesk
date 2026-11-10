using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FlowDesk.Web.ViewModels.WorkItems;

public class CreateWorkItemViewModel
{
    [Required(ErrorMessage = "عنوان تسک را وارد کنید.")]
    [StringLength(
        200,
        ErrorMessage = "عنوان حداکثر ۲۰۰ کاراکتر است.")]
    [Display(Name = "عنوان تسک")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "یک پروژه انتخاب کنید.")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "پروژهٔ انتخاب‌شده معتبر نیست.")]
    [Display(Name = "پروژه")]
    public int? ProjectId { get; set; }

    [Required(ErrorMessage = "یک مسئول انتخاب کنید.")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "مسئول انتخاب‌شده معتبر نیست.")]
    [Display(Name = "مسئول تسک")]
    public int? AssignedProjectMemberId { get; set; }

    [BindNever]
    [ValidateNever]
    public List<SelectListItem> Projects { get; set; } = new();

    [BindNever]
    [ValidateNever]
    public List<SelectListItem> Members { get; set; } = new();
}