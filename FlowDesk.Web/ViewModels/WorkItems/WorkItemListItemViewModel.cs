using FlowDesk.Domain.Enums;

namespace FlowDesk.Web.ViewModels.WorkItems;

public class WorkItemListItemViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    public WorkItemStatus Status { get; set; }

    public string StatusText { get; set; } = string.Empty;
}