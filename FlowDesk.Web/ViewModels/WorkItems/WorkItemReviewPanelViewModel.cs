using FlowDesk.Application.DTOs.WorkItems;

namespace FlowDesk.Web.ViewModels.WorkItems;

public sealed record WorkItemReviewPanelViewModel(int TaskId, string? BackUrl, WorkItemReviewPage Review);
