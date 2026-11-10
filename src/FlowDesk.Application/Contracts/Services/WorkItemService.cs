using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.WorkItems;
using FlowDesk.Domain.Entities;
using FlowDesk.Domain.Enums;
using System.Linq.Expressions;
using FlowDesk.Application.Common;

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

    public async Task ChangeStatusAsync(
    int id,
    WorkItemStatus targetStatus)
    {
        var workItem = await _workItemRepository.GetByIdAsync(id);

        if (workItem is null)
        {
            throw new KeyNotFoundException("تسک موردنظر پیدا نشد.");
        }

        switch (targetStatus)
        {
            case WorkItemStatus.InProgress:
                workItem.Start();
                break;

            case WorkItemStatus.Done:
                workItem.Complete();
                break;

            case WorkItemStatus.ToDo:
                workItem.Reopen();
                break;

            default:
                throw new ArgumentException(
                    "وضعیت انتخاب‌شده معتبر نیست.",
                    nameof(targetStatus));
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<WorkItemListItemDto?> GetByIdAsync(int id)
    {
        var workItem = await _workItemRepository.GetByIdAsync(id);

        if (workItem is null)
        {
            return null;
        }

        return new WorkItemListItemDto
        {
            Id = workItem.Id,
            Title = workItem.Title,
            ProjectId = workItem.ProjectId,
            Status = workItem.Status
        };
    }

    public async Task ChangeTitleAsync(int id, string title)
    {
        var workItem = await _workItemRepository.GetByIdAsync(id);

        if (workItem is null)
        {
            throw new KeyNotFoundException("تسک پیدا نشد.");
        }

        workItem.ChangeTitle(title);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<PagedResult<WorkItemListItemDto>> SearchAsync(
    WorkItemSearchDto input)
    {
        if (input.ProjectId.HasValue && input.ProjectId.Value <= 0)
        {
            throw new ArgumentException(
                "شناسهٔ پروژه معتبر نیست.", nameof(input.ProjectId));
        }

        if (input.Status.HasValue &&
            !Enum.IsDefined(typeof(WorkItemStatus), input.Status.Value))
        {
            throw new ArgumentException(
                "وضعیت معتبر نیست.", nameof(input.Status));
        }

        const int pageSize = 10;

        string searchText = input.SearchText?.Trim() ?? string.Empty;
        int? projectId = input.ProjectId;
        WorkItemStatus? status = input.Status;

        Expression<Func<WorkItem, bool>> predicate = item =>
            (searchText == "" || item.Title.Contains(searchText)) &&
            (!projectId.HasValue || item.ProjectId == projectId.Value) &&
            (!status.HasValue || item.Status == status.Value);

        int totalCount = await _workItemRepository.CountAsync(predicate);

        int totalPages = Math.Max(
            1, (int)Math.Ceiling(totalCount / (double)pageSize));

        int pageNumber = Math.Clamp(input.PageNumber, 1, totalPages);

        var workItems = await _workItemRepository.GetPageAsync(
            predicate, pageNumber, pageSize);

        var items = workItems.Select(item => new WorkItemListItemDto
        {
            Id = item.Id,
            Title = item.Title,
            ProjectId = item.ProjectId,
            Status = item.Status
        }).ToList();

        return new PagedResult<WorkItemListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}