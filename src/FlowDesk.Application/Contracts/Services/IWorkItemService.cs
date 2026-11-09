using FlowDesk.Application.DTOs.WorkItems;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Application.Contracts.Services;

public interface IWorkItemService
{
    Task<int> CreateAsync(CreateWorkItemDto input);
    Task<List<WorkItemListItemDto>> GetAllAsync();
    Task ChangeStatusAsync(int id, WorkItemStatus targetStatus);
    Task<WorkItemListItemDto?> GetByIdAsync(int id);
    Task ChangeTitleAsync(int id, string title);
}