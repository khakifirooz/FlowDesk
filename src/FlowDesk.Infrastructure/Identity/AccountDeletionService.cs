using System.Data;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FlowDesk.Infrastructure.Identity;

public enum AccountDeletionResult
{
    Deleted,
    InvalidPassword,
    ManagesProjects,
    LinkedToProjects,
    Conflict
}

public sealed class AccountDeletionService(
    FlowDeskDbContext projects,
    FlowDeskIdentityDbContext identity,
    UserManager<ApplicationUser> users)
{
    public async Task<AccountDeletionResult> DeleteAsync(
        ApplicationUser user, string password, CancellationToken cancellationToken)
    {
        if (!await users.CheckPasswordAsync(user, password))
            return AccountDeletionResult.InvalidPassword;

        // Both contexts use the same physical database. Share one connection and transaction
        // so a failed Identity delete cannot leave the user's invitations partially removed.
        projects.Database.SetDbConnection(identity.Database.GetDbConnection());
        await using var transaction = await identity.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        await using var projectTransaction = await projects.Database.UseTransactionAsync(
            transaction.GetDbTransaction(), cancellationToken);

        if (await projects.Projects.AnyAsync(x => x.ManagerUserId == user.Id, cancellationToken))
            return AccountDeletionResult.ManagesProjects;

        if (await projects.ProjectMembers.AnyAsync(x => x.AccountUserId == user.Id, cancellationToken))
            return AccountDeletionResult.LinkedToProjects;

        var normalizedEmail = user.NormalizedEmail;
        await projects.ProjectInvitations
            .Where(x => x.PendingUserId == user.Id ||
                (normalizedEmail != null && x.NormalizedEmail == normalizedEmail))
            .ExecuteDeleteAsync(cancellationToken);

        if (!(await users.DeleteAsync(user)).Succeeded)
            return AccountDeletionResult.Conflict;

        await transaction.CommitAsync(cancellationToken);
        return AccountDeletionResult.Deleted;
    }
}
