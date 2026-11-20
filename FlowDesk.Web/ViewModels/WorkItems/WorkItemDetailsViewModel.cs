using FlowDesk.Application.DTOs.WorkItems;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Web.ViewModels.WorkItems;

public sealed record WorkItemDetailsViewModel(
    WorkItemDetailsDto Details, string ProjectName, string AssigneeName, string? BackUrl, DateOnly Today);

public static class WorkItemLabels
{
    public static string Priority(WorkItemPriority priority) => priority switch
    {
        WorkItemPriority.Low => "کم",
        WorkItemPriority.High => "زیاد",
        WorkItemPriority.Urgent => "فوری",
        _ => "عادی"
    };

    public static (WorkItemStatus Target, string Text) NextStatus(WorkItemStatus status) => status switch
    {
        WorkItemStatus.ToDo => (WorkItemStatus.InProgress, "شروع"),
        WorkItemStatus.InProgress => (WorkItemStatus.Done, "تکمیل"),
        _ => (WorkItemStatus.ToDo, "بازگشایی")
    };

    public static string Status(WorkItemStatus status) => status switch
    {
        WorkItemStatus.ToDo => "انجام‌نشده",
        WorkItemStatus.InProgress => "در حال انجام",
        WorkItemStatus.Done => "انجام‌شده",
        _ => "نامشخص"
    };
}
