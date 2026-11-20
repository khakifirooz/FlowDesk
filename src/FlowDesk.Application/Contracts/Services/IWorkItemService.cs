using FlowDesk.Application.DTOs.WorkItems;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Application.Contracts.Services;

public interface IWorkItemService
{
    Task<int> CreateAsync(CreateWorkItemDto input);

    Task<List<WorkItemListItemDto>> GetAllAsync();

    Task<WorkItemListItemDto?> GetByIdAsync(int id);

    Task<List<WorkItemListItemDto>> GetByProjectIdAsync(int projectId);

    Task<WorkItemSearchResult> SearchAsync(
        WorkItemSearchDto input);

    Task ChangeTitleAsync(int id, string title);

    Task EditAsync(int id, EditWorkItemDto input);

    Task<WorkItemDetailsDto?> GetDetailsAsync(int id, int activityPage);

    Task SetArchivedAsync(int id, bool archived);

    Task AddCommentAsync(int id, string? text, string authorName);

    Task ChangeStatusAsync(int id, WorkItemStatus targetStatus, string? authorName = null);

    Task<bool> CompleteAsync(int id, string? note, string authorName);

    Task ChangeAssigneeAsync(
        int id,
        int projectId,
        int projectMemberId);
}