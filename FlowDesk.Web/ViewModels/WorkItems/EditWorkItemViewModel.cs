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
}