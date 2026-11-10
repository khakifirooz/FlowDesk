using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities;

public class ProjectMember : BaseEntity
{
    public int ProjectId { get; private set; }

    public int TeamMemberId { get; private set; }

    public ProjectMember(int projectId, int teamMemberId)
    {
        if (projectId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(projectId));
        }

        if (teamMemberId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(teamMemberId));
        }

        ProjectId = projectId;
        TeamMemberId = teamMemberId;
    }
}