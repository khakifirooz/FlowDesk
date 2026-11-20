using System.Security.Cryptography;
using FlowDesk.Application.Common;
using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Contracts.Services;

public sealed record InvitationTarget(int ProjectId, int MembershipId, string ProjectName,
    string MemberName, bool IsLinked, bool HasActiveInvite, string? InvitedEmail = null,
    string? PendingUserId = null, Guid? Version = null);
public sealed record ManagerInvitationState(int MembershipId, string InvitedEmail, bool IsPending);
public sealed record IssuedInvitation(string Token, DateTimeOffset ExpiresAtUtc, Guid Version);
public enum InvitationState { Unavailable, WrongAccount, AccountAlreadyInProject, Ready, PendingApproval }
public sealed record InvitationOutcome(InvitationState State, int? ProjectId = null, string? ProjectName = null);

public sealed class ProjectInvitationService(
    IRepository<ProjectInvitation> invitations,
    IRepository<ProjectMember> memberships,
    IRepository<Project> projects,
    IRepository<TeamMember> teamMembers,
    ProjectAccess access,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<InvitationTarget?> GetTargetAsync(int projectId, int membershipId)
    {
        var member = await GetManagedMemberAsync(projectId, membershipId);
        if (member is null) return null;
        var teamMember = await teamMembers.GetByIdAsync(member.TeamMemberId);
        var project = await projects.GetByIdAsync(projectId);
        if (project is null || teamMember is null) return null;
        var invitation = await FindByMembershipAsync(projectId, membershipId);
        var now = clock.GetUtcNow();
        bool active = invitation?.IsAvailable(now) == true;
        return new(projectId, membershipId, project.Name, teamMember.FullName,
            member.AccountUserId is not null, active, active ? invitation?.NormalizedEmail : null,
            invitation?.IsPendingApproval(now) == true ? invitation.PendingUserId : null, invitation?.Version);
    }

    public async Task<IssuedInvitation> IssueAsync(int projectId, int membershipId, string normalizedEmail)
    {
        if (string.IsNullOrWhiteSpace(normalizedEmail) || normalizedEmail.Length > 256)
            throw new InvalidOperationException("ایمیل دعوت معتبر نیست.");

        var member = await GetManagedMemberAsync(projectId, membershipId);
        if (member is null)
            throw new KeyNotFoundException("عضو پیدا نشد.");
        if (member.AccountUserId is not null)
            throw new InvalidOperationException("این عضو به یک حساب متصل است.");

        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        string token = Convert.ToHexString(bytes);
        string hash = Convert.ToHexString(SHA256.HashData(bytes));
        var expires = clock.GetUtcNow().AddDays(3);
        var invitation = await FindByMembershipAsync(projectId, membershipId);
        if (invitation?.IsPendingApproval(clock.GetUtcNow()) == true)
            throw new InvalidOperationException("ابتدا درخواست فعلی را تأیید یا رد کنید.");

        if (invitation is null)
        {
            invitation = new(projectId, membershipId, normalizedEmail, hash, expires);
            await invitations.AddAsync(invitation);
        }
        else
            invitation.Issue(normalizedEmail, hash, expires);

        await unitOfWork.SaveChangesAsync();
        return new(token, expires, invitation.Version);
    }

    public async Task RevokeAsync(int projectId, int membershipId, Guid version)
    {
        if (await GetManagedMemberAsync(projectId, membershipId) is null)
            throw new KeyNotFoundException("عضو پیدا نشد.");
        var invitation = await FindByMembershipAsync(projectId, membershipId);
        var now = clock.GetUtcNow();
        if (invitation is null || !invitation.IsAvailable(now) || invitation.Version != version)
            throw new InvalidOperationException("دعوت تغییر کرده است. صفحه را دوباره باز کنید.");
        invitation.Revoke(now);
        await unitOfWork.SaveChangesAsync();
    }

    public async Task<InvitationOutcome> OpenAsync(string? token, string userId, string? normalizedEmail, bool accept = false)
    {
        if (token is null || token.Length != 64 || !token.All(Uri.IsHexDigit))
            return new(InvitationState.Unavailable);

        string hash = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(token)));
        var invitation = (await invitations.FindAsync(x => x.TokenHash == hash)).SingleOrDefault();
        var now = clock.GetUtcNow();
        if (invitation?.IsAvailable(now) != true)
            return new(InvitationState.Unavailable);

        if (!string.Equals(invitation.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
            return new(InvitationState.WrongAccount);

        var member = await memberships.GetByIdAsync(invitation.MembershipId);
        if (member is null || member.ProjectId != invitation.ProjectId ||
            (member.AccountUserId is not null && member.AccountUserId != userId))
            return new(InvitationState.Unavailable);

        if (await memberships.CountAsync(x => x.ProjectId == invitation.ProjectId &&
            x.Id != invitation.MembershipId && x.AccountUserId == userId) > 0)
            return new(InvitationState.AccountAlreadyInProject);

        var project = await projects.GetByIdAsync(invitation.ProjectId);
        if (project is null) return new(InvitationState.Unavailable);
        if (invitation.IsPendingApproval(now))
            return string.Equals(invitation.PendingUserId, userId, StringComparison.Ordinal)
                ? new(InvitationState.PendingApproval, project.Id, project.Name)
                : new(InvitationState.Unavailable);
        if (!accept) return new(InvitationState.Ready, project.Id, project.Name);

        invitation.RequestMembership(userId, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync();
        return new(InvitationState.PendingApproval, project.Id, project.Name);
    }

    public async Task<IReadOnlyList<ManagerInvitationState>> GetManagerInvitesAsync(int projectId)
    {
        (await access.GetViewScopeAsync()).RequireManager(projectId);
        var now = clock.GetUtcNow();
        return (await invitations.FindAsync(x => x.ProjectId == projectId && x.RevokedAtUtc == null &&
                x.AcceptedAtUtc == null))
            .Where(x => x.IsAvailable(now))
            .Select(x => new ManagerInvitationState(x.MembershipId, x.NormalizedEmail, x.IsPendingApproval(now)))
            .ToArray();
    }

    public async Task ApproveAsync(int projectId, int membershipId, string pendingUserId, string? normalizedEmail, Guid version)
    {
        var member = await GetManagedMemberAsync(projectId, membershipId)
            ?? throw new KeyNotFoundException("عضو پروژه پیدا نشد.");

        var invitation = await FindByMembershipAsync(projectId, membershipId);
        var now = clock.GetUtcNow();
        if (invitation is null || invitation.Version != version ||
            !invitation.IsPendingApproval(now) || member.AccountUserId is not null ||
            !string.Equals(invitation.PendingUserId, pendingUserId, StringComparison.Ordinal) ||
            !string.Equals(invitation.NormalizedEmail, normalizedEmail, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("اطلاعات درخواست تغییر کرده است. صفحه را دوباره باز کنید.");
        }

        if (await memberships.CountAsync(x => x.ProjectId == projectId &&
            x.Id != membershipId && x.AccountUserId == pendingUserId) > 0)
            throw new InvalidOperationException("این حساب از قبل عضو همین پروژه است.");

        member.LinkAccount(pendingUserId);
        invitation.Approve(pendingUserId, now);
        await unitOfWork.SaveChangesAsync();
    }

    private async Task<ProjectMember?> GetManagedMemberAsync(int projectId, int membershipId)
    {
        (await access.GetScopeAsync()).RequireManager(projectId);
        var member = await memberships.GetByIdAsync(membershipId);
        return member?.ProjectId == projectId ? member : null;
    }

    private async Task<ProjectInvitation?> FindByMembershipAsync(int projectId, int membershipId) =>
        (await invitations.FindAsync(x => x.ProjectId == projectId && x.MembershipId == membershipId)).SingleOrDefault();
}
