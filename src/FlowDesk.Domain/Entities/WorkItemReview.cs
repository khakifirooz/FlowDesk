using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities;

public sealed class WorkItemReview : BaseEntity
{
    public const int MaxScore = 10;
    public const int FeedbackMaxLength = 1000;

    private WorkItemReview() { }

    public WorkItemReview(int workItemId, int completionActivityId, int recipientMembershipId,
        string recipientName, string? recipientUserId, int? score, string? feedback,
        string reviewerUserId, string reviewerName, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workItemId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(completionActivityId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recipientMembershipId);
        WorkItemId = workItemId;
        CompletionActivityId = completionActivityId;
        RecipientMembershipId = recipientMembershipId;
        RecipientName = TextRules.RequiredName(recipientName, nameof(recipientName), "نام دریافت‌کننده");
        RecipientUserId = recipientUserId;
        Update(score, feedback, reviewerUserId, reviewerName, now);
    }

    public int WorkItemId { get; private set; }
    public int CompletionActivityId { get; private set; }
    public int RecipientMembershipId { get; private set; }
    public string RecipientName { get; private set; } = string.Empty;
    public string? RecipientUserId { get; private set; }
    public int? Score { get; private set; }
    public string? Feedback { get; private set; }
    public string ReviewerUserId { get; private set; } = string.Empty;
    public string ReviewerName { get; private set; } = string.Empty;
    public DateTimeOffset ReviewedAtUtc { get; private set; }
    public Guid Revision { get; private set; }

    public void Update(int? score, string? feedback, string reviewerUserId, string reviewerName, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewerUserId);
        var body = feedback?.Trim();
        if (score is < 0 or > MaxScore)
            throw new ArgumentOutOfRangeException(nameof(score), "نمره باید بین ۰ و ۱۰ باشد.");
        if (body?.Length > FeedbackMaxLength)
            throw new ArgumentException("بازخورد حداکثر ۱۰۰۰ کاراکتر است.", nameof(feedback));
        if (!score.HasValue && string.IsNullOrEmpty(body))
            throw new ArgumentException("نمره یا نظر خود را وارد کنید.", nameof(feedback));

        ReviewerName = TextRules.RequiredName(reviewerName, nameof(reviewerName), "نام مدیر پروژه");
        ReviewerUserId = reviewerUserId;
        Score = score;
        Feedback = string.IsNullOrEmpty(body) ? null : body;
        ReviewedAtUtc = now;
        Revision = Guid.NewGuid();
    }
}
