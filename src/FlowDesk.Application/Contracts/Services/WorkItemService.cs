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
    private readonly IRepository<WorkItemActivity> _activity;
    private readonly TimeProvider _clock;

    public WorkItemService(IRepository<WorkItem> items,
        IRepository<ProjectMember> members, IUnitOfWork unitOfWork, ProjectAccess access,
        IRepository<WorkItemActivity> activity, TimeProvider clock)
    {
        _items = items;
        _members = members;
        _unitOfWork = unitOfWork;
        _access = access;
        _activity = activity;
        _clock = clock;
    }

    public async Task<int> CreateAsync(CreateWorkItemDto input)
    {
        var scope = await _access.GetScopeAsync();
        scope.RequireManager(input.ProjectId);
        var item = new WorkItem(input.Title, input.ProjectId);
        item.ChangeDetails(input.Title, input.Description, input.Priority, input.DueDate);
        await RequireUniqueTitleAsync(item.ProjectId, item.Title);
        item.AssignTo(await RequireMembershipAsync(input.ProjectId, input.AssignedProjectMemberId));
        item.RecordCreation(_clock.GetUtcNow());
        await _items.AddAsync(item);
        await SaveWithActivityAsync(item, scope, WorkItemActivityKind.Created);
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
        if (item is null || !scope.VisibleProjectIds.Contains(item.ProjectId)) return null;
        return Map(item, scope);
    }

    public async Task<List<WorkItemListItemDto>> GetByProjectIdAsync(int projectId)
    {
        var scope = await _access.GetScopeAsync();
        scope.RequireView(projectId);
        var items = await _items.FindAsync(x => x.ProjectId == projectId && x.ArchivedAtUtc == null);
        return items.OrderByDescending(x => x.Id).Select(x => Map(x, scope)).ToList();
    }

    public async Task<WorkItemSearchResult> SearchAsync(WorkItemSearchDto input)
    {
        if (input.ProjectId is <= 0)
            throw new ArgumentException("شناسهٔ پروژه معتبر نیست.");
        if (input.Status.HasValue && !Enum.IsDefined(input.Status.Value))
            throw new ArgumentException("وضعیت معتبر نیست.");

        if (input.Priority.HasValue && !Enum.IsDefined(input.Priority.Value))
            throw new ArgumentException("اولویت معتبر نیست.");
        var scope = await _access.GetViewScopeAsync();
        if (input.Focus.HasValue && (!Enum.IsDefined(input.Focus.Value) || input.Archived))
            throw new ArgumentException("فیلتر انتخاب‌شده برای این فهرست معتبر نیست.");
        if (input.ProjectId is int requestedProject)
        {
            if (input.ManagedOnly) scope.RequireManager(requestedProject);
            else scope.RequireView(requestedProject);
        }

        var ids = input.ManagedOnly ? scope.ManagedProjectIds : scope.VisibleProjectIds;
        var ownIds = scope.OwnMembershipIds;
        string searchText = TextRules.Normalize(input.SearchText);
        if (searchText.Length > TextRules.NameMaxLength)
            throw new ArgumentException("متن جست‌وجو حداکثر ۲۰۰ کاراکتر است.", nameof(input.SearchText));
        int? projectId = input.ProjectId;
        var status = input.Status;
        var priority = input.Priority;
        bool archived = input.Archived;
        bool onlyMine = input.OnlyMine;
        const int pageSize = 10;

        Expression<Func<WorkItem, bool>> scopedTasks = item =>
            ids.Contains(item.ProjectId) &&
            (archived ? item.ArchivedAtUtc != null : item.ArchivedAtUtc == null) &&
            (!onlyMine || (item.AssignedProjectMemberId.HasValue &&
                ownIds.Contains(item.AssignedProjectMemberId.Value)));
        Expression<Func<WorkItem, bool>> filters = item =>
            (!projectId.HasValue || item.ProjectId == projectId) &&
            (!status.HasValue || item.Status == status) &&
            (!priority.HasValue || item.Priority == priority);
        var predicate = And(scopedTasks, filters);

        foreach (string term in searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Expression<Func<WorkItem, bool>> containsTerm = item => item.Title.Contains(term);
            predicate = And(predicate, containsTerm);
        }

        if (input.Focus is { } focus)
        {
            DateOnly today = FlowDeskCalendar.Today(_clock);
            DateOnly weekEnd = today.AddDays(6);
            Expression<Func<WorkItem, bool>> attention = focus switch
            {
                WorkItemFocus.Overdue => item => item.DueDate < today,
                WorkItemFocus.DueToday => item => item.DueDate == today,
                WorkItemFocus.NextSevenDays => item => item.DueDate >= today && item.DueDate <= weekEnd,
                WorkItemFocus.WithDeadline => item => item.DueDate != null,
                WorkItemFocus.Unassigned => item => item.AssignedProjectMemberId == null,
                _ => throw new ArgumentOutOfRangeException(nameof(input.Focus))
            };
            predicate = And(And(predicate, item => item.Status != WorkItemStatus.Done), attention);
        }

        int totalCount = await _items.CountAsync(predicate);
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        int pageNumber = Math.Clamp(input.PageNumber, 1, totalPages);
        var items = totalCount == 0
            ? new List<WorkItem>()
            : input.Focus is WorkItemFocus.Overdue or WorkItemFocus.DueToday
                or WorkItemFocus.NextSevenDays or WorkItemFocus.WithDeadline
                ? await _items.GetPageOrderedAsync(predicate, item => item.DueDate, pageNumber, pageSize)
                : await _items.GetPageAsync(predicate, pageNumber, pageSize);

        int? personalCount = null;
        if (onlyMine && totalCount == 0)
        {
            bool filtersApplied = projectId.HasValue || status.HasValue || priority.HasValue || input.Focus.HasValue || searchText.Length > 0;
            personalCount = filtersApplied && ownIds.Count > 0
                ? await _items.CountAsync(scopedTasks)
                : 0;
        }

        return new WorkItemSearchResult
        {
            HasLinkedMembership = ownIds.Count > 0,
            PersonalCountWithoutFilters = personalCount,
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
        RequireVisibleTask(scope, item);
        scope.RequireManager(item.ProjectId);
        string cleanTitle = TextRules.RequiredName(title, nameof(title), "عنوان تسک");
        await RequireUniqueTitleAsync(item.ProjectId, cleanTitle, id);
        item.ChangeTitle(cleanTitle);
        await SaveWithActivityAsync(item, scope, WorkItemActivityKind.Edited);
    }

    public async Task ChangeStatusAsync(int id, WorkItemStatus targetStatus, string? authorName = null)
    {
        var item = await RequireItemAsync(id);
        var scope = await _access.GetScopeAsync();
        RequireVisibleTask(scope, item);
        scope.RequireStatusChange(item);
        var now = _clock.GetUtcNow();

        WorkItemActivityKind kind;
        switch (targetStatus)
        {
            case WorkItemStatus.InProgress:
                item.Start(); kind = WorkItemActivityKind.Started; break;
            case WorkItemStatus.Done:
                item.Complete(now); kind = WorkItemActivityKind.Completed; break;
            case WorkItemStatus.ToDo:
                item.Reopen(); kind = WorkItemActivityKind.Reopened; break;
            default: throw new ArgumentException("وضعیت انتخاب‌شده معتبر نیست.");
        }
        await GuardMemberWriteAsync(scope, item, assigneeOnly: true);
        await SaveWithActivityAsync(item, scope, kind,
            authorName: kind == WorkItemActivityKind.Completed ? authorName : null, occurredAtUtc: now);
    }

    public async Task<bool> CompleteAsync(int id, string? note, string authorName)
    {
        var item = await RequireItemAsync(id);
        var scope = await _access.GetScopeAsync();
        RequireVisibleTask(scope, item);
        scope.RequireStatusChange(item);

        if (item.ArchivedAtUtc.HasValue)
            throw new InvalidOperationException("برای تکمیل این تسک، ابتدا آن را از بایگانی برگردانید.");

        if (item.Status == WorkItemStatus.Done)
            return false;

        var now = _clock.GetUtcNow();
        var completion = new WorkItemActivity(item, WorkItemActivityKind.Completed, scope.UserId,
            now, note, authorName);
        item.ConfirmCompletion(now);
        await GuardMemberWriteAsync(scope, item, assigneeOnly: true);
        await _activity.AddAsync(completion);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task ChangeAssigneeAsync(int id, int projectId, int projectMemberId)
    {
        var scope = await _access.GetScopeAsync();
        scope.RequireManager(projectId);
        var item = await RequireItemAsync(id);
        if (item.ProjectId != projectId)
            throw new KeyNotFoundException("تسک در این پروژه پیدا نشد.");
        item.AssignTo(await RequireMembershipAsync(projectId, projectMemberId));
        await SaveWithActivityAsync(item, scope, WorkItemActivityKind.Reassigned);
    }

    public async Task EditAsync(int id, EditWorkItemDto input)
    {
        var scope = await _access.GetScopeAsync();
        var item = await RequireItemAsync(id);
        RequireVisibleTask(scope, item);
        scope.RequireManager(item.ProjectId);
        var title = TextRules.RequiredName(input.Title, nameof(input.Title), "عنوان تسک");
        await RequireUniqueTitleAsync(item.ProjectId, title, id);
        item.ChangeDetails(title, input.Description, input.Priority, input.DueDate);
        await SaveWithActivityAsync(item, scope, WorkItemActivityKind.Edited);
    }

    public async Task<WorkItemDetailsDto?> GetDetailsAsync(int id, int activityPage)
    {
        var scope = await _access.GetScopeAsync();
        var item = await _items.GetByIdAsync(id);
        if (item is null || !scope.VisibleProjectIds.Contains(item.ProjectId)) return null;
        const int pageSize = 20;
        Expression<Func<WorkItemActivity, bool>> filter = x => x.WorkItemId == item.Id;
        int count = await _activity.CountAsync(filter);
        int pages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        int page = Math.Clamp(activityPage, 1, pages);
        var activity = await _activity.GetPageAsync(filter, page, pageSize);
        WorkItemActivity? latestCompletion = null;
        if (item.Status == WorkItemStatus.Done)
        {
            latestCompletion = page == 1
                ? activity.FirstOrDefault(x => x.Kind == WorkItemActivityKind.Completed)
                : null;
            latestCompletion ??= (await _activity.GetPageAsync(
                x => x.WorkItemId == item.Id && x.Kind == WorkItemActivityKind.Completed, 1, 1)).SingleOrDefault();
        }

        return new WorkItemDetailsDto(Map(item, scope), activity.Select(MapActivity).ToArray(), page, pages,
            latestCompletion is null ? null : MapActivity(latestCompletion));
    }

    public async Task SetArchivedAsync(int id, bool archived)
    {
        var scope = await _access.GetScopeAsync();
        var item = await RequireItemAsync(id);
        RequireVisibleTask(scope, item);
        scope.RequireManager(item.ProjectId);
        if (archived) item.Archive(_clock.GetUtcNow());
        else item.Restore();
        await SaveWithActivityAsync(item, scope,
            archived ? WorkItemActivityKind.Archived : WorkItemActivityKind.Restored);
    }

    public async Task AddCommentAsync(int id, string? text, string authorName)
    {
        var scope = await _access.GetScopeAsync();
        var item = await RequireItemAsync(id);
        RequireVisibleTask(scope, item);
        var comment = new WorkItemActivity(item, WorkItemActivityKind.Comment, scope.UserId,
            _clock.GetUtcNow(), text, authorName);
        item.CommentAdded();
        await GuardMemberWriteAsync(scope, item, assigneeOnly: false);
        await _activity.AddAsync(comment);
        await _unitOfWork.SaveChangesAsync();
    }

    private static void RequireVisibleTask(ProjectScope scope, WorkItem item)
    {
        if (!scope.VisibleProjectIds.Contains(item.ProjectId))
            throw new KeyNotFoundException("تسک پیدا نشد.");
    }

    private async Task GuardMemberWriteAsync(ProjectScope scope, WorkItem item, bool assigneeOnly)
    {
        if (scope.CanManage(item.ProjectId))
            return;

        var requiredId = item.AssignedProjectMemberId;
        var links = await _members.FindAsync(x => x.ProjectId == item.ProjectId &&
            x.AccountUserId == scope.UserId && (!assigneeOnly || x.Id == requiredId));
        var membership = links.SingleOrDefault();
        if (membership is null || !string.Equals(membership.AccountUserId, scope.UserId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("عضویت لازم برای انجام این کار معتبر نیست.");

        _unitOfWork.VerifyMembershipOnSave(membership);
    }

    private async Task SaveWithActivityAsync(WorkItem item, ProjectScope scope, WorkItemActivityKind kind,
        string? authorName = null, DateTimeOffset? occurredAtUtc = null)
    {
        await _activity.AddAsync(new WorkItemActivity(item, kind, scope.UserId,
            occurredAtUtc ?? _clock.GetUtcNow(), authorName: authorName));
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

    private static Expression<Func<WorkItem, bool>> And(
        Expression<Func<WorkItem, bool>> first, Expression<Func<WorkItem, bool>> second)
    {
        var parameter = first.Parameters[0];
        var body = Expression.AndAlso(first.Body, Expression.Invoke(second, parameter));
        return Expression.Lambda<Func<WorkItem, bool>>(body, parameter);
    }

    private static WorkItemActivityDto MapActivity(WorkItemActivity activity) =>
        new(activity.Kind, activity.OccurredAtUtc, activity.AuthorName, activity.Message);

    private static WorkItemListItemDto Map(WorkItem item, ProjectScope scope) => new()
    {
        Id = item.Id,
        Title = item.Title,
        ProjectId = item.ProjectId,
        Status = item.Status,
        AssignedProjectMemberId = item.AssignedProjectMemberId,
        CanManage = scope.CanManage(item.ProjectId),
        CanChangeStatus = item.ArchivedAtUtc is null && scope.CanChangeStatus(item),
        Description = item.Description,
        Priority = item.Priority,
        DueDate = item.DueDate,
        CreatedAtUtc = item.CreatedAtUtc,
        CompletedAtUtc = item.CompletedAtUtc,
        ArchivedAtUtc = item.ArchivedAtUtc
    };
}
