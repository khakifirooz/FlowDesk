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
    private readonly IRepository<WorkItem> _items;
    private readonly IRepository<ProjectMember> _members;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProjectAccess _access;

    public WorkItemService(IRepository<WorkItem> items,
        IRepository<ProjectMember> members, IUnitOfWork unitOfWork, ProjectAccess access)
    {
        _items = items;
        _members = members;
        _unitOfWork = unitOfWork;
        _access = access;
    }

    public async Task<int> CreateAsync(CreateWorkItemDto input)
    {
        (await _access.GetScopeAsync()).RequireManager(input.ProjectId);
        var item = new WorkItem(input.Title, input.ProjectId);
        await RequireUniqueTitleAsync(item.ProjectId, item.Title);
        item.AssignTo(await RequireMembershipAsync(input.ProjectId, input.AssignedProjectMemberId));
        await _items.AddAsync(item);
        await _unitOfWork.SaveChangesAsync();
        return item.Id;
    }

    public async Task<List<WorkItemListItemDto>> GetAllAsync()
    {
        var scope = await _access.GetScopeAsync();
        var ids = scope.VisibleProjectIds;
        var items = await _items.FindAsync(x => ids.Contains(x.ProjectId));
        return items.OrderByDescending(x => x.Id).Select(x => Map(x, scope)).ToList();
    }

    public async Task<WorkItemListItemDto?> GetByIdAsync(int id)
    {
        var scope = await _access.GetScopeAsync();
        var item = await _items.GetByIdAsync(id);
        if (item is null) return null;
        scope.RequireView(item.ProjectId);
        return Map(item, scope);
    }

    public async Task<List<WorkItemListItemDto>> GetByProjectIdAsync(int projectId)
    {
        var scope = await _access.GetScopeAsync();
        scope.RequireView(projectId);
        var items = await _items.FindAsync(x => x.ProjectId == projectId);
        return items.OrderByDescending(x => x.Id).Select(x => Map(x, scope)).ToList();
    }

    public async Task<PagedResult<WorkItemListItemDto>> SearchAsync(WorkItemSearchDto input)
    {
        if (input.ProjectId is <= 0)
            throw new ArgumentException("شناسهٔ پروژه معتبر نیست.");
        if (input.Status.HasValue && !Enum.IsDefined(input.Status.Value))
            throw new ArgumentException("وضعیت معتبر نیست.");

        var scope = await _access.GetScopeAsync();
        if (input.ProjectId.HasValue) scope.RequireView(input.ProjectId.Value);

        var ids = scope.VisibleProjectIds;
        var ownIds = scope.OwnMembershipIds;
        string searchText = TextRules.Normalize(input.SearchText);
        int? projectId = input.ProjectId;
        var status = input.Status;
        bool onlyMine = input.OnlyMine;
        const int pageSize = 10;

        Expression<Func<WorkItem, bool>> predicate = item =>
            ids.Contains(item.ProjectId) &&
            (!onlyMine || (item.AssignedProjectMemberId.HasValue &&
                ownIds.Contains(item.AssignedProjectMemberId.Value))) &&
            (searchText == "" || item.Title.Contains(searchText)) &&
            (!projectId.HasValue || item.ProjectId == projectId.Value) &&
            (!status.HasValue || item.Status == status.Value);

        int totalCount = await _items.CountAsync(predicate);
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        int pageNumber = Math.Clamp(input.PageNumber, 1, totalPages);
        var items = await _items.GetPageAsync(predicate, pageNumber, pageSize);

        return new PagedResult<WorkItemListItemDto>
        {
            Items = items.Select(x => Map(x, scope)).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task ChangeTitleAsync(int id, string title)
    {
        var scope = await _access.GetScopeAsync();
        var item = await RequireItemAsync(id);
        scope.RequireManager(item.ProjectId);
        string cleanTitle = TextRules.RequiredName(title, nameof(title), "عنوان تسک");
        await RequireUniqueTitleAsync(item.ProjectId, cleanTitle, id);
        item.ChangeTitle(cleanTitle);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ChangeStatusAsync(int id, WorkItemStatus targetStatus)
    {
        var scope = await _access.GetScopeAsync();
        var item = await RequireItemAsync(id);
        scope.RequireView(item.ProjectId);
        if (!scope.CanChangeStatus(item)) throw new UnauthorizedAccessException();

        switch (targetStatus)
        {
            case WorkItemStatus.InProgress: item.Start(); break;
            case WorkItemStatus.Done: item.Complete(); break;
            case WorkItemStatus.ToDo: item.Reopen(); break;
            default: throw new ArgumentException("وضعیت انتخاب‌شده معتبر نیست.");
        }
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ChangeAssigneeAsync(int id, int projectId, int projectMemberId)
    {
        var scope = await _access.GetScopeAsync();
        scope.RequireManager(projectId);
        var item = await RequireItemAsync(id);
        if (item.ProjectId != projectId)
            throw new InvalidOperationException("تسک متعلق به این پروژه نیست.");
        item.AssignTo(await RequireMembershipAsync(projectId, projectMemberId));
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<WorkItem> RequireItemAsync(int id) =>
        await _items.GetByIdAsync(id) ?? throw new KeyNotFoundException("تسک پیدا نشد.");

    private async Task<ProjectMember> RequireMembershipAsync(int projectId, int memberId)
    {
        var membership = await _members.GetByIdAsync(memberId);
        if (membership is null || membership.ProjectId != projectId)
            throw new InvalidOperationException("مسئول باید عضو همین پروژه باشد.");
        return membership;
    }

    private async Task RequireUniqueTitleAsync(int projectId, string title, int exceptId = 0)
    {
        if (await _items.CountAsync(x => x.ProjectId == projectId &&
            x.Title == title && x.Id != exceptId) > 0)
            throw new InvalidOperationException("تسک دیگری با این عنوان در همین پروژه وجود دارد.");
    }

    private static WorkItemListItemDto Map(WorkItem item, ProjectScope scope) => new()
    {
        Id = item.Id,
        Title = item.Title,
        ProjectId = item.ProjectId,
        Status = item.Status,
        AssignedProjectMemberId = item.AssignedProjectMemberId,
        CanManage = scope.CanManage(item.ProjectId),
        CanChangeStatus = scope.CanChangeStatus(item)
    };
}
