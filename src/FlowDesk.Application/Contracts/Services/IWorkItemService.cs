using FlowDesk.Application.DTOs.WorkItems;

namespace FlowDesk.Application.Contracts.Services;

public interface IWorkItemService
{
    Task<int> CreateAsync(CreateWorkItemDto input);
    Task<List<WorkItemListItemDto>> GetAllAsync();
}