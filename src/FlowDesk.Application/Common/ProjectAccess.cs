using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Common;

public interface ICurrentUser
{
    string? UserId { get; }
}

public sealed class ProjectScope
{
    public required string UserId { get; init; }
    public List<int> ManagedProjectIds { get; init; } = new();
    public List<int> VisibleProjectIds { get; init; } = new();
    public List<int> OwnMembershipIds { get; init; } = new();

    public bool CanManage(int projectId) => ManagedProjectIds.Contains(projectId);

    public bool CanChangeStatus(WorkItem item) => CanManage(item.ProjectId) ||
        (item.AssignedProjectMemberId is int id && OwnMembershipIds.Contains(id));

    public void RequireStatusChange(WorkItem item)
    {
        RequireView(item.ProjectId);
        if (!CanChangeStatus(item))
            throw new UnauthorizedAccessException("فقط مسئول تسک یا مدیر همین پروژه می‌تواند وضعیت آن را تغییر دهد.");
    }

    public void RequireTeamDirectory()
    {
        if (ManagedProjectIds.Count == 0)
            throw new UnauthorizedAccessException("فهرست افراد فقط برای مدیران پروژه در دسترس است.");
    }

    public void RequireView(int projectId)
    {
        if (!VisibleProjectIds.Contains(projectId))
            throw new UnauthorizedAccessException("به این پروژه دسترسی ندارید.");
    }

    public void RequireManager(int projectId)
    {
        if (!CanManage(projectId))
            throw new UnauthorizedAccessException("این عملیات فقط برای مدیر پروژه مجاز است.");
    }
}

public sealed class ProjectAccess
{
    private readonly IRepository<Project> _projects;
    private readonly IRepository<ProjectMember> _memberships;
    private readonly ICurrentUser _currentUser;

    public ProjectAccess(IRepository<Project> projects,
        IRepository<ProjectMember> memberships, ICurrentUser currentUser)
    {
        _projects = projects;
        _memberships = memberships;
        _currentUser = currentUser;
    }

    public async Task RequireTeamMemberSelectionAsync(ProjectScope scope, int teamMemberId)
    {
        scope.RequireTeamDirectory();
        var links = await _memberships.FindAsync(x => x.TeamMemberId == teamMemberId);
        if (links.Count > 0 && !links.Any(x => scope.CanManage(x.ProjectId)))
            throw new UnauthorizedAccessException("این فرد در فهرست اعضای قابل انتخاب برای پروژه‌های شما نیست.");
    }

    public async Task RequireTeamMemberEditAsync(ProjectScope scope, int teamMemberId)
    {
        scope.RequireTeamDirectory();
        var managedProjectIds = scope.ManagedProjectIds;
        if (await _memberships.CountAsync(x =>
            x.TeamMemberId == teamMemberId && !managedProjectIds.Contains(x.ProjectId)) > 0)
        {
            throw new UnauthorizedAccessException("این فرد در پروژه‌ای دیگر عضویت دارد و نام او از اینجا قابل تغییر نیست.");
        }
    }

    private Task<ProjectScope>? _viewScope;

    public Task<ProjectScope> GetViewScopeAsync() => _viewScope ??= GetScopeAsync();

    public async Task<ProjectScope> GetScopeAsync()
    {
        string userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("ابتدا وارد حساب شوید.");

        var managed = await _projects.FindAsync(x => x.ManagerUserId == userId);
        var memberships = await _memberships.FindAsync(x => x.AccountUserId == userId);

        return new ProjectScope
        {
            UserId = userId,
            ManagedProjectIds = managed.Select(x => x.Id).ToList(),
            VisibleProjectIds = managed.Select(x => x.Id)
                .Concat(memberships.Select(x => x.ProjectId)).Distinct().ToList(),
            OwnMembershipIds = memberships.Select(x => x.Id).ToList()
        };
    }
}
