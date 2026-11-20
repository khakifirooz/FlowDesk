using FlowDesk.Application.Common;
using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.DTOs.WorkItems;
using FlowDesk.Domain.Entities;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Application.Services;

public sealed class WorkItemReviewService(
    IRepository<WorkItem> tasks, IRepository<WorkItemActivity> activities,
    IRepository<WorkItemReview> reviews, IRepository<ProjectMember> memberships,
    IRepository<TeamMember> people, ProjectAccess access, IUnitOfWork unitOfWork, TimeProvider clock)
{
    private sealed record Recipient(int MembershipId, string Name, string? UserId);

    public async Task<WorkItemReviewPage?> GetAsync(int taskId)
    {
        var scope = await access.GetViewScopeAsync();
        var task = await tasks.GetByIdAsync(taskId);
        if (task is null || !scope.VisibleProjectIds.Contains(task.ProjectId) || task.Status != WorkItemStatus.Done)
            return null;

        bool manager = scope.CanManage(task.ProjectId);
        var completion = await FindCompletionAsync(task.Id);
        if (completion is null)
            return manager && !task.ArchivedAtUtc.HasValue
                ? new(null, null, false, null, "این تسک سابقهٔ تحویل ندارد. برای ارزیابی، آن را بازگشایی و دوباره تکمیل کنید.")
                : null;

        var review = await FindReviewAsync(completion.Id);
        if (!manager)
        {
            if (review?.RecipientUserId is not { } userId ||
                !string.Equals(userId, scope.UserId, StringComparison.Ordinal)) return null;

            return new(null, review.RecipientName, false, Map(review), null);
        }

        var recipient = review is null && !task.ArchivedAtUtc.HasValue
            ? await ResolveRecipientAsync(task, completion, scope.UserId) : null;
        var name = review?.RecipientName ?? recipient?.Name;
        bool canSave = !task.ArchivedAtUtc.HasValue && name is not null;
        string? reason = canSave || task.ArchivedAtUtc.HasValue ? null :
            "مسئول این تحویل مشخص نیست. برای ثبت ارزیابی، مسئول را مشخص کنید و تسک را دوباره تحویل دهید.";
        return new(completion.Id, name, canSave, review is null ? null : Map(review), reason);
    }

    public async Task SaveAsync(int taskId, SaveWorkItemReview input, string reviewerName)
    {
        var task = await tasks.GetByIdAsync(taskId);
        var scope = await access.GetScopeAsync();
        if (task is null || !scope.VisibleProjectIds.Contains(task.ProjectId))
            throw new KeyNotFoundException("تسک پیدا نشد.");

        scope.RequireManager(task.ProjectId);
        var completion = task.Status == WorkItemStatus.Done ? await FindCompletionAsync(task.Id) : null;
        if (completion is null || completion.Id != input.SubmissionId)
            throw new InvalidOperationException("تحویل تسک تغییر کرده است. صفحه را دوباره باز کنید.");

        task.ReviewRecorded();
        var review = await FindReviewAsync(completion.Id);
        if (review?.Revision != input.Revision)
            throw new InvalidOperationException("ارزیابی تغییر کرده است. صفحه را دوباره باز کنید.");

        var now = clock.GetUtcNow();
        if (review is null)
        {
            var recipient = await ResolveRecipientAsync(task, completion, scope.UserId)
                ?? throw new InvalidOperationException("مسئول تحویل مشخص نیست و نمی‌توان ارزیابی ثبت کرد.");

            await reviews.AddAsync(new(task.Id, completion.Id, recipient.MembershipId, recipient.Name,
                recipient.UserId, input.Score, input.Feedback, scope.UserId, reviewerName, now));
        }
        else
        {
            review.Update(input.Score, input.Feedback, scope.UserId, reviewerName, now);
        }

        await unitOfWork.SaveChangesAsync();
    }

    private async Task<WorkItemActivity?> FindCompletionAsync(int taskId) =>
        (await activities.GetPageAsync(x => x.WorkItemId == taskId && x.Kind == WorkItemActivityKind.Completed,
            1, 1)).SingleOrDefault();

    private async Task<WorkItemReview?> FindReviewAsync(int completionId) =>
        (await reviews.FindAsync(x => x.CompletionActivityId == completionId)).SingleOrDefault();

    private async Task<Recipient?> ResolveRecipientAsync(WorkItem task, WorkItemActivity completion, string managerId)
    {
        if (!completion.CompletionRecipientCaptured)
        {
            var latestId = completion.Id;
            if (await activities.CountAsync(x => x.WorkItemId == task.Id && x.Id > latestId &&
                x.Kind == WorkItemActivityKind.Reassigned) > 0) return null;
        }

        int? memberId = completion.CompletionRecipientCaptured
            ? completion.CompletionRecipientMembershipId : task.AssignedProjectMemberId;

        if (memberId is not int id) return null;
        var link = await memberships.GetByIdAsync(id);
        if (link is null || link.ProjectId != task.ProjectId) return null;

        if (!completion.CompletionRecipientCaptured &&
            !string.Equals(completion.ActorUserId, managerId, StringComparison.Ordinal) &&
            !string.Equals(completion.ActorUserId, link.AccountUserId, StringComparison.Ordinal))
            return null;

        var recipient = await people.GetByIdAsync(link.TeamMemberId);
        return recipient is null ? null : new(id, recipient.FullName, completion.CompletionRecipientCaptured
            ? completion.CompletionRecipientUserId : link.AccountUserId);
    }

    private static WorkItemReviewDto Map(WorkItemReview review) =>
        new(review.RecipientName, review.Score, review.Feedback, review.ReviewerName, review.ReviewedAtUtc, review.Revision);
}
