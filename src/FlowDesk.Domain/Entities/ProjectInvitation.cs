using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities;

public sealed class ProjectInvitation : BaseEntity
{
    private ProjectInvitation() { }

    public ProjectInvitation(int projectId, int membershipId, string email, string tokenHash, DateTimeOffset expiresAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(projectId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(membershipId);
        ProjectId = projectId;
        MembershipId = membershipId;
        Issue(email, tokenHash, expiresAt);
    }

    public int ProjectId { get; private set; }
    public int MembershipId { get; private set; }
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid Version { get; private set; }

    public bool IsAvailable(DateTimeOffset now) =>
        AcceptedAtUtc is null && RevokedAtUtc is null && now < ExpiresAtUtc;

    public void Issue(string normalizedEmail, string tokenHash, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        NormalizedEmail = normalizedEmail;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAt;
        AcceptedAtUtc = null;
        RevokedAtUtc = null;
        Version = Guid.NewGuid();
    }

    public void Accept(DateTimeOffset now)
    {
        if (!IsAvailable(now))
            throw new InvalidOperationException("این دعوت دیگر قابل استفاده نیست.");

        AcceptedAtUtc = now;
        Version = Guid.NewGuid();
    }

    public void Revoke(DateTimeOffset now)
    {
        if (!IsAvailable(now)) return;
        RevokedAtUtc = now;
        Version = Guid.NewGuid();
    }
}
