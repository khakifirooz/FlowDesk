using System.Security.Cryptography;
using FlowDesk.Application.Common;
using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Contracts.Services;

public sealed record InvitationTarget(int ProjectId, int MembershipId, string ProjectName, string MemberName, bool IsLinked, bool HasActiveInvite);
public sealed record IssuedInvitation(string Token, DateTimeOffset ExpiresAtUtc);
public enum InvitationState { Unavailable, WrongAccount, AccountAlreadyInProject, Ready, Accepted }
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
        return new(projectId, membershipId, project.Name, teamMember.FullName,
            member.AccountUserId is not null, invitation?.IsAvailable(clock.GetUtcNow()) == true);
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
        if (invitation is null)
            await invitations.AddAsync(new(projectId, membershipId, normalizedEmail, hash, expires));
        else
            invitation.Issue(normalizedEmail, hash, expires);

        await unitOfWork.SaveChangesAsync();
        return new(token, expires);
    }

    public async Task RevokeAsync(int projectId, int membershipId)
    {
        if (await GetManagedMemberAsync(projectId, membershipId) is null)
            throw new KeyNotFoundException("عضو پیدا نشد.");
        var invitation = await FindByMembershipAsync(projectId, membershipId);
        if (invitation is null) return;
        invitation.Revoke(clock.GetUtcNow());
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
        if (!accept) return new(InvitationState.Ready, project.Id, project.Name);

        var acceptedAt = clock.GetUtcNow();
        if (!invitation.IsAvailable(acceptedAt)) return new(InvitationState.Unavailable);

        member.LinkAccount(userId);
        invitation.Accept(acceptedAt);
        await unitOfWork.SaveChangesAsync();
        return new(InvitationState.Accepted, project.Id, project.Name);
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
