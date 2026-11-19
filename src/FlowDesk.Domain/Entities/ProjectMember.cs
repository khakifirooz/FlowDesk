using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities;

public class ProjectMember : BaseEntity
{
    public int ProjectId { get; private set; }
    public int TeamMemberId { get; private set; }
    public string? AccountUserId { get; private set; }

    public ProjectMember(int projectId, int teamMemberId)
    {
        if (projectId <= 0)
            throw new ArgumentOutOfRangeException(nameof(projectId));

        if (teamMemberId <= 0)
            throw new ArgumentOutOfRangeException(nameof(teamMemberId));

        ProjectId = projectId;
        TeamMemberId = teamMemberId;
    }

    public void LinkAccount(string accountUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountUserId);

        if (accountUserId.Length > 450)
            throw new ArgumentException("شناسهٔ حساب معتبر نیست.", nameof(accountUserId));

        if (AccountUserId is not null &&
            !string.Equals(AccountUserId, accountUserId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "این عضو به حساب دیگری متصل است. ابتدا اتصال قبلی را قطع کنید.");
        }

        AccountUserId = accountUserId;
    }

    public void UnlinkAccount()
    {
        AccountUserId = null;
    }
}