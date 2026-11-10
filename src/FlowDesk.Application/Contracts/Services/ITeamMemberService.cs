using FlowDesk.Application.DTOs.TeamMembers;

namespace FlowDesk.Application.Contracts.Services;

public interface ITeamMemberService
{
    Task<int> CreateAsync(CreateTeamMemberDto input);

    Task<List<TeamMemberListItemDto>> GetAllAsync();

    Task<TeamMemberListItemDto?> GetByIdAsync(int id);

    Task RenameAsync(int id, string fullName);

    Task DeleteAsync(int id);
}