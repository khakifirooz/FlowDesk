using FlowDesk.Application.DTOs.TeamMembers;

namespace FlowDesk.Application.Contracts.Services;

public interface ITeamMemberService
{
    Task<int> CreateAsync(CreateTeamMemberDto input);

    Task<List<TeamMemberListItemDto>> GetAllAsync();

    Task DeleteAsync(int id);
}