using FlowDesk.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace FlowDesk.Web.ViewModels.WorkItems;

public class EditWorkItemViewModel
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان تسک را وارد کنید.")]
    [StringLength(
        200,
        ErrorMessage = "حداکثر ۲۰۰ کاراکتر مجاز است.")]
    [Display(Name = "عنوان تسک")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "شرح تسک حداکثر ۲۰۰۰ کاراکتر است.")]
    [Display(Name = "شرح تسک")]
    public string? Description { get; set; }

    [EnumDataType(typeof(WorkItemPriority), ErrorMessage = "اولویت معتبر نیست.")]
    [Display(Name = "اولویت")]
    public WorkItemPriority Priority { get; set; }

    [Display(Name = "مهلت")]
    [DataType(DataType.Date)]
    public DateOnly? DueDate { get; set; }

}