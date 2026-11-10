using System.ComponentModel.DataAnnotations;
using FlowDesk.Application.DTOs.WorkItems;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FlowDesk.Web.ViewModels.WorkItems;

public class AssignWorkItemViewModel
{
    [Required(ErrorMessage = "یک پروژه انتخاب کنید.")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "پروژهٔ انتخاب‌شده معتبر نیست.")]
    [Display(Name = "پروژه")]
    public int? ProjectId { get; set; }

    [Required(ErrorMessage = "یک تسک انتخاب کنید.")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "تسک انتخاب‌شده معتبر نیست.")]
    [Display(Name = "تسک")]
    public int? Id { get; set; }

    [Required(ErrorMessage = "یک مسئول انتخاب کنید.")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "مسئول انتخاب‌شده معتبر نیست.")]
    [Display(Name = "مسئول")]
    public int? AssignedProjectMemberId { get; set; }

    [BindNever]
    [ValidateNever]
    public List<SelectListItem> Projects { get; set; } = new();

    [BindNever]
    [ValidateNever]
    public List<WorkItemListItemDto> WorkItems { get; set; } = new();

    [BindNever]
    [ValidateNever]
    public List<SelectListItem> Members { get; set; } = new();
}