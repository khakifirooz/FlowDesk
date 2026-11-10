using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.ProjectMembers;
using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Services;

public class ProjectMemberService : IProjectMemberService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<TeamMember> _teamMemberRepository;
    private readonly IRepository<ProjectMember> _projectMemberRepository;
    private readonly IRepository<WorkItem> _workItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectMemberService(
        IRepository<Project> projectRepository,
        IRepository<TeamMember> teamMemberRepository,
        IRepository<ProjectMember> projectMemberRepository,
        IRepository<WorkItem> workItemRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _teamMemberRepository = teamMemberRepository;
        _projectMemberRepository = projectMemberRepository;
        _workItemRepository = workItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ProjectMemberListItemDto>>
        GetByProjectIdAsync(int projectId)
    {
        var memberships = await _projectMemberRepository.FindAsync(
            x => x.ProjectId == projectId);

        if (memberships.Count == 0)
            return new List<ProjectMemberListItemDto>();

        var memberIds = memberships
            .Select(x => x.TeamMemberId)
            .ToList();

        var members = await _teamMemberRepository.FindAsync(
            x => memberIds.Contains(x.Id));

        var namesById = members.ToDictionary(
            x => x.Id,
            x => x.FullName);

        return memberships
            .Select(x => new ProjectMemberListItemDto
            {
                Id = x.Id,
                TeamMemberId = x.TeamMemberId,
                FullName = namesById[x.TeamMemberId]
            })
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.Id)
            .ToList();
    }

    public async Task AddAsync(int projectId, int teamMemberId)
    {
        if (!await _projectRepository.ExistsAsync(projectId))
            throw new KeyNotFoundException("Project was not found.");

        if (!await _teamMemberRepository.ExistsAsync(teamMemberId))
            throw new InvalidOperationException(
                "فرد انتخاب‌شده وجود ندارد.");

        int duplicateCount = await _projectMemberRepository.CountAsync(
            x => x.ProjectId == projectId &&
                 x.TeamMemberId == teamMemberId);

        if (duplicateCount > 0)
            throw new InvalidOperationException(
                "این فرد قبلاً به پروژه اضافه شده است.");

        var membership = new ProjectMember(projectId, teamMemberId);

        await _projectMemberRepository.AddAsync(membership);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveAsync(int projectId, int membershipId)
    {
        var membership = await _projectMemberRepository
            .GetByIdAsync(membershipId);

        if (membership is null || membership.ProjectId != projectId)
            throw new KeyNotFoundException(
                "Project membership was not found.");

        int assignedTaskCount = await _workItemRepository.CountAsync(
            x => x.AssignedProjectMemberId == membershipId);

        if (assignedTaskCount > 0)
            throw new InvalidOperationException(
                "این عضو تسک واگذارشده دارد. ابتدا مسئول تسک‌ها را تغییر بده یا واگذاری آن‌ها را لغو کن.");

        _projectMemberRepository.Remove(membership);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Dictionary<int, string>> GetNamesByIdsAsync(
    List<int> membershipIds)
    {
        if (membershipIds.Count == 0)
            return new Dictionary<int, string>();

        var memberships = await _projectMemberRepository.FindAsync(
            x => membershipIds.Contains(x.Id));

        if (memberships.Count == 0)
            return new Dictionary<int, string>();

        var memberIds = memberships
            .Select(x => x.TeamMemberId)
            .Distinct()
            .ToList();

        var members = await _teamMemberRepository.FindAsync(
            x => memberIds.Contains(x.Id));

        var namesByMemberId = members.ToDictionary(
            x => x.Id,
            x => x.FullName);

        return memberships.ToDictionary(
            x => x.Id,
            x => namesByMemberId[x.TeamMemberId]);
    }
}