using FlowDesk.Application.Common;

namespace FlowDesk.Application.DTOs.WorkItems;

public sealed class WorkItemSearchResult : PagedResult<WorkItemListItemDto>
{
    public bool HasLinkedMembership { get; init; }
    public int? PersonalCountWithoutFilters { get; init; }
}
