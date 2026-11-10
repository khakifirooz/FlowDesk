using FlowDesk.Application.DTOs.ProjectMembers;

namespace FlowDesk.Application.Contracts.Services;

public interface IProjectMemberService
{
    Task<List<ProjectMemberListItemDto>> GetByProjectIdAsync(int projectId);

    Task AddAsync(int projectId, int teamMemberId);

    Task RemoveAsync(int projectId, int membershipId);
}