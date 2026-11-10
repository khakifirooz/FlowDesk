using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.TeamMembers;
using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Services;

public class TeamMemberService : ITeamMemberService
{
    private readonly IRepository<TeamMember> _memberRepository;
    private readonly IRepository<ProjectMember> _projectMemberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TeamMemberService(
        IRepository<TeamMember> memberRepository,
        IRepository<ProjectMember> projectMemberRepository,
        IUnitOfWork unitOfWork)
    {
        _memberRepository = memberRepository;
        _projectMemberRepository = projectMemberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CreateAsync(CreateTeamMemberDto input)
    {
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
        var members = await _memberRepository.GetAllAsync();

        return members
            .OrderBy(member => member.FullName)
            .ThenBy(member => member.Id)
            .Select(member => new TeamMemberListItemDto
            {
                Id = member.Id,
                FullName = member.FullName
            })
            .ToList();
    }

    public async Task DeleteAsync(int id)
    {
        var member = await _memberRepository.GetByIdAsync(id);

        if (member is null)
        {
            throw new KeyNotFoundException("عضو پیدا نشد.");
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