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

    public async Task<List<ProjectMemberListItemDto>> GetByProjectIdAsync(
        int projectId)
    {
        var memberships = await _projectMemberRepository.FindAsync(
            membership => membership.ProjectId == projectId);

        if (memberships.Count == 0)
        {
            return new List<ProjectMemberListItemDto>();
        }

        var memberIds = memberships
            .Select(membership => membership.TeamMemberId)
            .Distinct()
            .ToList();

        var members = await _teamMemberRepository.FindAsync(
            member => memberIds.Contains(member.Id));

        var namesById = members.ToDictionary(
            member => member.Id,
            member => member.FullName);

        return memberships
            .Select(membership => new ProjectMemberListItemDto
            {
                Id = membership.Id,
                TeamMemberId = membership.TeamMemberId,
                FullName = namesById[membership.TeamMemberId]
            })
            .OrderBy(member => member.FullName)
            .ThenBy(member => member.Id)
            .ToList();
    }

    public async Task AddAsync(
        int projectId,
        int teamMemberId)
    {
        bool projectExists =
            await _projectRepository.ExistsAsync(projectId);

        if (!projectExists)
        {
            throw new KeyNotFoundException(
                "پروژه پیدا نشد.");
        }

        bool memberExists =
            await _teamMemberRepository.ExistsAsync(teamMemberId);

        if (!memberExists)
        {
            throw new InvalidOperationException(
                "فرد انتخاب‌شده وجود ندارد.");
        }

        int duplicateCount = await _projectMemberRepository.CountAsync(
            membership =>
                membership.ProjectId == projectId &&
                membership.TeamMemberId == teamMemberId);

        if (duplicateCount > 0)
        {
            throw new InvalidOperationException(
                "این فرد قبلاً به پروژه اضافه شده است.");
        }

        var membership = new ProjectMember(
            projectId,
            teamMemberId);

        await _projectMemberRepository.AddAsync(membership);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveAsync(
        int projectId,
        int membershipId)
    {
        var membership = await _projectMemberRepository
            .GetByIdAsync(membershipId);

        if (membership is null ||
            membership.ProjectId != projectId)
        {
            throw new KeyNotFoundException(
                "عضویت موردنظر در این پروژه پیدا نشد.");
        }

        int assignedTaskCount = await _workItemRepository.CountAsync(
            workItem =>
                workItem.AssignedProjectMemberId == membershipId);

        if (assignedTaskCount > 0)
        {
            throw new InvalidOperationException(
                "این عضو مسئول یک یا چند تسک است. ابتدا مسئول تمام تسک‌های او را به عضو دیگری تغییر دهید.");
        }

        _projectMemberRepository.Remove(membership);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Dictionary<int, string>> GetNamesByIdsAsync(
        List<int> membershipIds)
    {
        if (membershipIds.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var memberships = await _projectMemberRepository.FindAsync(
            membership => membershipIds.Contains(membership.Id));

        if (memberships.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var memberIds = memberships
            .Select(membership => membership.TeamMemberId)
            .Distinct()
            .ToList();

        var members = await _teamMemberRepository.FindAsync(
            member => memberIds.Contains(member.Id));

        var namesByMemberId = members.ToDictionary(
            member => member.Id,
            member => member.FullName);

        return memberships.ToDictionary(
            membership => membership.Id,
            membership => namesByMemberId[membership.TeamMemberId]);
    }
}