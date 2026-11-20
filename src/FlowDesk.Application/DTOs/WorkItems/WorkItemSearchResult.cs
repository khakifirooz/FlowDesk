using FlowDesk.Application.Common;

namespace FlowDesk.Application.DTOs.WorkItems;

public sealed class WorkItemSearchResult : PagedResult<WorkItemListItemDto>
{
    public int? ArchiveCountBeforeFilters { get; init; }
    public bool HasLinkedMembership { get; init; }
    public int? PersonalCountWithoutFilters { get; init; }
}
