using FlowDesk.Domain.Entities;
using FlowDesk.Infrastructure.Identity;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FlowDesk.Tests;

public sealed class AccountDeletionTests
{
    private const string Password = "Sandbox-Only1!";
    private static readonly DateTimeOffset Today = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DeletesAccountAndItsInvitationButPreservesSharedRecords()
    {
        await using var db = await TestDatabase.CreateAsync();
        var user = await db.AddUserAsync();
        await db.AddProjectAsync(user, manages: false, linked: false);

        Assert.Equal(AccountDeletionResult.Deleted, await db.DeleteAsync(user));
        Assert.Empty(await db.Identity.Users.AsNoTracking().ToListAsync());
        Assert.Equal("OTHER@EXAMPLE.NET", (await db.Projects.ProjectInvitations.SingleAsync()).NormalizedEmail);
        Assert.Equal(2, await db.Projects.ProjectMembers.CountAsync());
        Assert.Equal(1, await db.Projects.Projects.CountAsync());
        Assert.Equal(1, await db.Projects.WorkItems.CountAsync());
    }

    [Theory]
    [InlineData(true, false, AccountDeletionResult.ManagesProjects)]
    [InlineData(false, true, AccountDeletionResult.LinkedToProjects)]
    public async Task RejectsProjectsThatStillDependOnTheAccount(
        bool manages, bool linked, AccountDeletionResult expected)
    {
        await using var db = await TestDatabase.CreateAsync();
        var user = await db.AddUserAsync();
        await db.AddProjectAsync(user, manages, linked);

        Assert.Equal(expected, await db.DeleteAsync(user));
        Assert.Equal(1, await db.Identity.Users.CountAsync());
        Assert.Equal(2, await db.Projects.ProjectInvitations.CountAsync());
    }

    [Fact]
    public async Task WrongPasswordLeavesAccountAndInvitationsUntouched()
    {
        await using var db = await TestDatabase.CreateAsync();
        var user = await db.AddUserAsync();
        await db.AddProjectAsync(user, manages: false, linked: false);

        Assert.Equal(AccountDeletionResult.InvalidPassword, await db.DeleteAsync(user, "wrong"));
        Assert.Equal(1, await db.Identity.Users.CountAsync());
        Assert.Equal(2, await db.Projects.ProjectInvitations.CountAsync());
    }

    [Fact]
    public async Task IdentityConflictRollsBackInvitationRemoval()
    {
        await using var db = await TestDatabase.CreateAsync();
        var user = await db.AddUserAsync();
        await db.AddProjectAsync(user, manages: false, linked: false);
        await db.Identity.Users.Where(x => x.Id == user.Id)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.ConcurrencyStamp, "new-stamp"));

        Assert.Equal(AccountDeletionResult.Conflict, await db.DeleteAsync(user));
        Assert.Equal(1, await db.Identity.Users.CountAsync());
        Assert.Equal(2, await db.Projects.ProjectInvitations.CountAsync());
    }

    private sealed class TestDatabase(SqliteConnection connection, ServiceProvider provider, AsyncServiceScope scope)
        : IAsyncDisposable
    {
        private UserManager<ApplicationUser> Users => scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        public FlowDeskIdentityDbContext Identity => scope.ServiceProvider.GetRequiredService<FlowDeskIdentityDbContext>();
        public FlowDeskDbContext Projects => scope.ServiceProvider.GetRequiredService<FlowDeskDbContext>();

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            connection.CreateCollation("Latin1_General_100_CI_AS", StringComparer.OrdinalIgnoreCase.Compare);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<FlowDeskDbContext>(o => o.UseSqlite(connection));
            services.AddDbContext<FlowDeskIdentityDbContext>(o => o.UseSqlite(connection));
            services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<FlowDeskIdentityDbContext>();
            services.AddScoped<AccountDeletionService>();
            var provider = services.BuildServiceProvider();
            var scope = provider.CreateAsyncScope();
            var db = new TestDatabase(connection, provider, scope);
            await db.Identity.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
            await db.Projects.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
            return db;
        }

        public async Task<ApplicationUser> AddUserAsync()
        {
            var user = new ApplicationUser { FullName = "Test user", UserName = "user@example.net", Email = "user@example.net" };
            Assert.True((await Users.CreateAsync(user, Password)).Succeeded);
            return user;
        }

        public async Task AddProjectAsync(ApplicationUser user, bool manages, bool linked)
        {
            var project = Project.CreateManaged("Example project", manages ? user.Id : "another-manager");
            var person = new TeamMember("Test user");
            var other = new TeamMember("Another member");
            Projects.AddRange(project, person, other);
            await Projects.SaveChangesAsync();

            var membership = new ProjectMember(project.Id, person.Id);
            var otherMembership = new ProjectMember(project.Id, other.Id);
            if (linked) membership.LinkAccount(user.Id);
            var task = new WorkItem("Shared work", project.Id);
            Projects.AddRange(membership, otherMembership, task);
            await Projects.SaveChangesAsync();
            task.AssignTo(membership);
            var related = new ProjectInvitation(project.Id, membership.Id, user.NormalizedEmail!, "test-hash-1", Today.AddHours(1));
            related.RequestMembership(user.Id, Today);
            var unrelated = new ProjectInvitation(project.Id, otherMembership.Id, "OTHER@EXAMPLE.NET", "test-hash-2", Today.AddHours(1));
            Projects.AddRange(related, unrelated);
            await Projects.SaveChangesAsync();
        }

        public Task<AccountDeletionResult> DeleteAsync(ApplicationUser user, string password = Password) =>
            scope.ServiceProvider.GetRequiredService<AccountDeletionService>().DeleteAsync(user, password, CancellationToken.None);

        public async ValueTask DisposeAsync()
        {
            await scope.DisposeAsync();
            await provider.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
