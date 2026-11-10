using System.ComponentModel.DataAnnotations;
using FlowDesk.Domain.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FlowDesk.Web.ViewModels.WorkItems;

public class WorkItemIndexViewModel
{
    public string? SearchText { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "پروژه معتبر نیست.")]
    public int? ProjectId { get; set; }

    [EnumDataType(
        typeof(WorkItemStatus),
        ErrorMessage = "وضعیت معتبر نیست.")]
    public WorkItemStatus? Status { get; set; }

    [BindNever]
    [ValidateNever]
    public List<SelectListItem> Projects { get; set; } = new();

    [BindNever]
    [ValidateNever]
    public List<WorkItemListItemViewModel> Items { get; set; } = new();

    public int PageNumber { get; set; } = 1;

    [BindNever]
    public int TotalCount { get; set; }

    [BindNever]
    public int PageSize { get; set; } = 10;

    public int TotalPages =>
        Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}