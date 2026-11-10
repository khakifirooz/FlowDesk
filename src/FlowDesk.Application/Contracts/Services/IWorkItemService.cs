using FlowDesk.Application.Common;
using FlowDesk.Application.DTOs.WorkItems;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Application.Contracts.Services;

public interface IWorkItemService
{
    Task<int> CreateAsync(CreateWorkItemDto input);

    Task<List<WorkItemListItemDto>> GetAllAsync();

    Task<WorkItemListItemDto?> GetByIdAsync(int id);

    Task<List<WorkItemListItemDto>> GetByProjectIdAsync(int projectId);

    Task<PagedResult<WorkItemListItemDto>> SearchAsync(
        WorkItemSearchDto input);

    Task ChangeTitleAsync(int id, string title);

    Task ChangeStatusAsync(
        int id,
        WorkItemStatus targetStatus);

    Task ChangeAssigneeAsync(
        int id,
        int projectId,
        int projectMemberId);
}