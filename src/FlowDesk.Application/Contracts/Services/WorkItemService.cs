using System.Linq.Expressions;
using FlowDesk.Application.Common;
using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.WorkItems;
using FlowDesk.Domain.Common;
using FlowDesk.Domain.Entities;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Application.Services;

public class WorkItemService : IWorkItemService
{
    private readonly IRepository<WorkItem> _workItemRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<ProjectMember> _projectMemberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WorkItemService(
        IRepository<WorkItem> workItemRepository,
        IRepository<Project> projectRepository,
        IRepository<ProjectMember> projectMemberRepository,
        IUnitOfWork unitOfWork)
    {
        _workItemRepository = workItemRepository;
        _projectRepository = projectRepository;
        _projectMemberRepository = projectMemberRepository;
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

        var workItem = new WorkItem(
            input.Title,
            input.ProjectId);

        int duplicateCount = await _workItemRepository.CountAsync(
            item =>
                item.ProjectId == input.ProjectId &&
                item.Title == workItem.Title);

        if (duplicateCount > 0)
        {
            throw new InvalidOperationException(
                "تسکی با این عنوان در همین پروژه وجود دارد.");
        }

        var membership = await RequireMembershipAsync(
            input.ProjectId,
            input.AssignedProjectMemberId);

        workItem.AssignTo(membership);

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
                Status = item.Status,
                AssignedProjectMemberId = item.AssignedProjectMemberId
            })
            .ToList();
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
            Status = workItem.Status,
            AssignedProjectMemberId = workItem.AssignedProjectMemberId
        };
    }

    public async Task<List<WorkItemListItemDto>> GetByProjectIdAsync(
        int projectId)
    {
        var workItems = await _workItemRepository.FindAsync(
            item => item.ProjectId == projectId);

        return workItems
            .OrderByDescending(item => item.Id)
            .Select(item => new WorkItemListItemDto
            {
                Id = item.Id,
                Title = item.Title,
                ProjectId = item.ProjectId,
                Status = item.Status,
                AssignedProjectMemberId = item.AssignedProjectMemberId
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
            throw new KeyNotFoundException(
                "تسک موردنظر پیدا نشد.");
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

    public async Task ChangeTitleAsync(int id, string title)
    {
        var workItem = await _workItemRepository.GetByIdAsync(id);

        if (workItem is null)
        {
            throw new KeyNotFoundException(
                "تسک پیدا نشد.");
        }

        string cleanTitle = TextRules.RequiredName(
            title,
            nameof(title),
            "عنوان تسک");

        int duplicateCount = await _workItemRepository.CountAsync(
            item =>
                item.Id != id &&
                item.ProjectId == workItem.ProjectId &&
                item.Title == cleanTitle);

        if (duplicateCount > 0)
        {
            throw new InvalidOperationException(
                "تسک دیگری با این عنوان در همین پروژه وجود دارد.");
        }

        workItem.ChangeTitle(cleanTitle);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ChangeAssigneeAsync(
        int id,
        int projectId,
        int projectMemberId)
    {
        var workItem = await _workItemRepository.GetByIdAsync(id);

        if (workItem is null)
        {
            throw new KeyNotFoundException(
                "تسک موردنظر پیدا نشد.");
        }

        if (workItem.ProjectId != projectId)
        {
            throw new InvalidOperationException(
                "تسک انتخاب‌شده متعلق به این پروژه نیست.");
        }

        var membership = await RequireMembershipAsync(
            projectId,
            projectMemberId);

        workItem.AssignTo(membership);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<PagedResult<WorkItemListItemDto>> SearchAsync(
        WorkItemSearchDto input)
    {
        if (input.ProjectId.HasValue &&
            input.ProjectId.Value <= 0)
        {
            throw new ArgumentException(
                "شناسهٔ پروژه معتبر نیست.",
                nameof(input.ProjectId));
        }

        if (input.Status.HasValue &&
            !Enum.IsDefined(
                typeof(WorkItemStatus),
                input.Status.Value))
        {
            throw new ArgumentException(
                "وضعیت معتبر نیست.",
                nameof(input.Status));
        }

        const int pageSize = 10;

        string searchText = TextRules.Normalize(input.SearchText);
        int? projectId = input.ProjectId;
        WorkItemStatus? status = input.Status;

        Expression<Func<WorkItem, bool>> predicate = item =>
            (searchText == "" || item.Title.Contains(searchText)) &&
            (!projectId.HasValue || item.ProjectId == projectId.Value) &&
            (!status.HasValue || item.Status == status.Value);

        int totalCount = await _workItemRepository.CountAsync(predicate);

        int totalPages = Math.Max(
            1,
            (int)Math.Ceiling(totalCount / (double)pageSize));

        int pageNumber = Math.Clamp(
            input.PageNumber,
            1,
            totalPages);

        var workItems = await _workItemRepository.GetPageAsync(
            predicate,
            pageNumber,
            pageSize);

        var items = workItems
            .Select(item => new WorkItemListItemDto
            {
                Id = item.Id,
                Title = item.Title,
                ProjectId = item.ProjectId,
                Status = item.Status,
                AssignedProjectMemberId = item.AssignedProjectMemberId
            })
            .ToList();

        return new PagedResult<WorkItemListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private async Task<ProjectMember> RequireMembershipAsync(
        int projectId,
        int membershipId)
    {
        if (membershipId <= 0)
        {
            throw new InvalidOperationException(
                "یک مسئول انتخاب کنید.");
        }

        var membership = await _projectMemberRepository
            .GetByIdAsync(membershipId);

        if (membership is null)
        {
            throw new InvalidOperationException(
                "عضویت انتخاب‌شده وجود ندارد؛ دوباره انتخاب کنید.");
        }

        if (membership.ProjectId != projectId)
        {
            throw new InvalidOperationException(
                "مسئول تسک باید عضو همین پروژه باشد.");
        }

        return membership;
    }
}