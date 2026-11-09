using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.WorkItems;
using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Services;

public class WorkItemService : IWorkItemService
{
    private readonly IRepository<WorkItem> _workItemRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WorkItemService(
        IRepository<WorkItem> workItemRepository,
        IRepository<Project> projectRepository,
        IUnitOfWork unitOfWork)
    {
        _workItemRepository = workItemRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CreateAsync(CreateWorkItemDto input)
    {
        bool projectExists =
            await _projectRepository.ExistsAsync(input.ProjectId);

        if (!projectExists)
        {
            throw new ArgumentException(
                "پروژهٔ انتخاب‌شده وجود ندارد.",
                nameof(input.ProjectId));
        }

        var workItem = new WorkItem(input.Title, input.ProjectId);

        await _workItemRepository.AddAsync(workItem);
        await _unitOfWork.SaveChangesAsync();

        return workItem.Id;
    }

    public async Task<List<WorkItemListItemDto>> GetAllAsync()
    {
        var workItems = await _workItemRepository.GetAllAsync();

        return workItems
            .OrderByDescending(item => item.Id)
            .Select(item => new WorkItemListItemDto
            {
                Id = item.Id,
                Title = item.Title,
                ProjectId = item.ProjectId,
                Status = item.Status
            })
            .ToList();
    }
}