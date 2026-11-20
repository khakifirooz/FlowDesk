using FlowDesk.Application.Common;
using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.TeamMembers;
using FlowDesk.Domain.Common;
using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Services;

public class TeamMemberService : ITeamMemberService
{
    private readonly IRepository<TeamMember> _memberRepository;
    private readonly IRepository<ProjectMember> _projectMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProjectAccess _access;

    public TeamMemberService(
        IRepository<TeamMember> memberRepository,
        IRepository<ProjectMember> projectMemberRepository,
        IUnitOfWork unitOfWork, ProjectAccess access)
    {
        _memberRepository = memberRepository;
        _projectMemberRepository = projectMemberRepository;
        _unitOfWork = unitOfWork;
        _access = access;
    }

    public async Task<int> CreateAsync(CreateTeamMemberDto input)
    {
        (await _access.GetScopeAsync()).RequireTeamDirectory();
        var member = new TeamMember(input.FullName);

        int duplicateCount = await _memberRepository.CountAsync(
            existing => existing.FullName == member.FullName);

        if (duplicateCount > 0)
        {
            throw new InvalidOperationException(
                "عضوی با این نام قبلاً ثبت شده است.");
        }

        await _memberRepository.AddAsync(member);
        await _unitOfWork.SaveChangesAsync();

        return member.Id;
    }

    public async Task<List<TeamMemberListItemDto>> GetAllAsync()
    {
        var scope = await _access.GetViewScopeAsync();
        scope.RequireTeamDirectory();
        var memberships = (await _projectMemberRepository.GetAllAsync()).ToLookup(x => x.TeamMemberId);
        var members = await _memberRepository.GetAllAsync();

        return members
            .Where(member => !memberships.Contains(member.Id)
                || memberships[member.Id].Any(link => scope.CanManage(link.ProjectId)))
            .OrderBy(member => member.FullName)
            .ThenBy(member => member.Id)
            .Select(member => new TeamMemberListItemDto
            {
                Id = member.Id,
                FullName = member.FullName,
                CanEdit = memberships[member.Id].All(link => scope.CanManage(link.ProjectId)),
                CanDelete = !memberships.Contains(member.Id)
            })
            .ToList();
    }

    public async Task<TeamMemberListItemDto?> GetByIdAsync(int id)
    {
        await _access.RequireTeamMemberEditAsync(await _access.GetScopeAsync(), id);
        var member = await _memberRepository.GetByIdAsync(id);

        if (member is null)
        {
            return null;
        }

        return new TeamMemberListItemDto
        {
            Id = member.Id,
            FullName = member.FullName
        };
    }

    public async Task RenameAsync(int id, string fullName)
    {
        await _access.RequireTeamMemberEditAsync(await _access.GetScopeAsync(), id);
        var member = await _memberRepository.GetByIdAsync(id);

        if (member is null)
        {
            throw new KeyNotFoundException(
                "عضو پیدا نشد.");
        }

        string cleanName = TextRules.RequiredName(
            fullName,
            nameof(fullName),
            "نام عضو");

        int duplicateCount = await _memberRepository.CountAsync(
            existing =>
                existing.Id != id &&
                existing.FullName == cleanName);

        if (duplicateCount > 0)
        {
            throw new InvalidOperationException(
                "عضو دیگری با این نام وجود دارد.");
        }

        member.Rename(cleanName);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        (await _access.GetScopeAsync()).RequireTeamDirectory();
        var member = await _memberRepository.GetByIdAsync(id);

        if (member is null)
        {
            throw new KeyNotFoundException(
                "عضو پیدا نشد.");
        }

        int membershipCount = await _projectMemberRepository.CountAsync(
            membership => membership.TeamMemberId == id);

        if (membershipCount > 0)
        {
            throw new InvalidOperationException(
                "این فرد عضو یک یا چند پروژه است و قابل حذف نیست.");
        }

        _memberRepository.Remove(member);

        await _unitOfWork.SaveChangesAsync();
    }
}