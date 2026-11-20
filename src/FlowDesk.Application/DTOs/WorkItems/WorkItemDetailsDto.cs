using FlowDesk.Domain.Entities;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Application.DTOs.WorkItems;

public sealed record EditWorkItemDto(string Title, string? Description, WorkItemPriority Priority, DateOnly? DueDate);
public sealed record WorkItemActivityDto(WorkItemActivityKind Kind, DateTimeOffset OccurredAtUtc, string? AuthorName, string? Message);
public sealed record WorkItemDetailsDto(WorkItemListItemDto Item, WorkItemActivityDto[] Activity,
    int ActivityPage, int ActivityPages);
