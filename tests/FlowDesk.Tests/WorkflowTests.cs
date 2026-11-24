using FlowDesk.Domain.Entities;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Tests;

public sealed class WorkflowTests
{
    private static readonly DateTimeOffset Today = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CompletionRequiresStartAndReopeningClearsCompletion()
    {
        var task = new WorkItem("Build the dashboard", 1);
        Assert.Throws<InvalidOperationException>(() => task.Complete(Today));
        Assert.Equal(WorkItemStatus.ToDo, task.Status);

        task.Start();
        task.Complete(Today);
        Assert.Equal(WorkItemStatus.Done, task.Status);
        Assert.Equal(Today, task.CompletedAtUtc);

        task.Reopen();
        Assert.Equal(WorkItemStatus.ToDo, task.Status);
        Assert.Null(task.CompletedAtUtc);
    }

    [Fact]
    public void ArchivedTaskCannotStartUntilRestored()
    {
        var task = new WorkItem("Build the dashboard", 1);
        task.Archive(Today);
        Assert.Throws<InvalidOperationException>(task.Start);
        task.Restore();
        task.Start();
        Assert.Equal(WorkItemStatus.InProgress, task.Status);
    }

    [Fact]
    public void InvitationOnlyAllowsTheRequesterToBeApproved()
    {
        var invitation = new ProjectInvitation(1, 2, "USER@EXAMPLE.NET", "test-hash", Today.AddHours(1));
        invitation.RequestMembership("requester", Today);

        Assert.Throws<InvalidOperationException>(() => invitation.Approve("someone-else", Today));
        Assert.True(invitation.IsPendingApproval(Today));
        invitation.Approve("requester", Today);
        Assert.Null(invitation.PendingUserId);
        Assert.False(invitation.IsAvailable(Today));
    }

    [Fact]
    public void InvitationCannotBeApprovedAtTheExpiryBoundary()
    {
        var invitation = new ProjectInvitation(1, 2, "USER@EXAMPLE.NET", "test-hash", Today.AddMinutes(1));
        invitation.RequestMembership("requester", Today);
        Assert.Throws<InvalidOperationException>(() => invitation.Approve("requester", Today.AddMinutes(1)));
        Assert.Null(invitation.AcceptedAtUtc);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void ReviewRejectsOutOfRangeScore(int score) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WorkItemReview(1, 2, 3, "Member", "member", score, "Feedback", "manager", "Manager", Today));

    [Fact]
    public void ReviewRequiresScoreOrNonblankFeedback()
    {
        Assert.Throws<ArgumentException>(() =>
            new WorkItemReview(1, 2, 3, "Member", "member", null, "   ", "manager", "Manager", Today));

        var review = new WorkItemReview(1, 2, 3, "Member", "member", 0, null, "manager", "Manager", Today);
        Assert.Equal(0, review.Score);
    }
}
