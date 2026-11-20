using System.ComponentModel.DataAnnotations;
using FlowDesk.Application.DTOs.WorkItems;
using FlowDesk.Domain.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FlowDesk.Web.ViewModels.WorkItems;

public class WorkItemIndexViewModel : IValidatableObject
{
    public bool OnlyMine { get; set; }
    public bool Archived { get; set; }
    public bool ManagedOnly { get; set; }

    [EnumDataType(typeof(WorkItemFocus), ErrorMessage = "فیلتر پیگیری معتبر نیست.")]
    public WorkItemFocus? Focus { get; set; }

    [BindNever]
    public bool HasManagedProjects { get; set; }

    [BindNever]
    public DateOnly Today { get; set; }

    public bool HasFilters => !string.IsNullOrWhiteSpace(SearchText) || ProjectId.HasValue
        || Status.HasValue || Priority.HasValue || Focus.HasValue;

    public string ScopeLabel => ManagedOnly
        ? OnlyMine ? "تسک‌های من در پروژه‌های تحت مدیریت" : "تسک‌های پروژه‌های تحت مدیریت"
        : OnlyMine ? "تسک‌های من" : "تسک‌های پروژه‌ها";


    [EnumDataType(typeof(WorkItemPriority), ErrorMessage = "اولویت معتبر نیست.")]
    public WorkItemPriority? Priority { get; set; }

    [BindNever]
    [ValidateNever]
    public bool CanCreate { get; set; }

    [BindNever]
    public bool HasLinkedMembership { get; set; }

    [BindNever]
    public int? PersonalCountWithoutFilters { get; set; }

    [StringLength(200, ErrorMessage = "متن جست‌وجو حداکثر ۲۰۰ کاراکتر است.")]
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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Archived && Focus.HasValue)
            yield return new ValidationResult("فیلتر پیگیری فقط در تسک‌های خارج از بایگانی کاربرد دارد.", [nameof(Focus)]);
    }

    public Dictionary<string, string> FilterRoute()
    {
        var route = new Dictionary<string, string>();
        if (OnlyMine) route[nameof(OnlyMine)] = "true";
        if (ManagedOnly) route[nameof(ManagedOnly)] = "true";
        if (Archived) route[nameof(Archived)] = "true";
        return route;
    }

    public Dictionary<string, string> PagingRoute()
    {
        var route = FilterRoute();
        if (!string.IsNullOrWhiteSpace(SearchText)) route[nameof(SearchText)] = SearchText;
        if (ProjectId.HasValue) route[nameof(ProjectId)] = ProjectId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (Priority.HasValue) route[nameof(Priority)] = Priority.Value.ToString();
        if (Status.HasValue) route[nameof(Status)] = Status.Value.ToString();
        if (Focus.HasValue) route[nameof(Focus)] = Focus.Value.ToString();
        return route;
    }
}