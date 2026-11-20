using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities;

public enum WorkItemActivityKind
{
    Created, Edited, Reassigned, Started, Completed, Reopened, Archived, Restored, Comment
}

public sealed class WorkItemActivity : BaseEntity
{
    public const int MessageMaxLength = 1000;

    private WorkItemActivity() { }

    public WorkItemActivity(WorkItem workItem, WorkItemActivityKind kind,
        string actorUserId, DateTimeOffset occurredAtUtc, string? message = null, string? authorName = null)
    {
        WorkItem = workItem;
        Kind = kind;
        ActorUserId = actorUserId;
        OccurredAtUtc = occurredAtUtc;
        if (kind == WorkItemActivityKind.Comment)
        {
            var body = message?.Trim();
            if (string.IsNullOrWhiteSpace(body) || body.Length > MessageMaxLength)
                throw new ArgumentException("متن نظر باید بین ۱ تا ۱۰۰۰ کاراکتر باشد.", nameof(message));
            Message = body;
            AuthorName = TextRules.RequiredName(authorName, nameof(authorName), "نام فرستنده");
        }
        else if (kind == WorkItemActivityKind.Completed)
        {
            var body = message?.Trim();
            if (body?.Length > MessageMaxLength)
                throw new ArgumentException("گزارش انجام کار حداکثر ۱۰۰۰ کاراکتر است.", nameof(message));
            Message = string.IsNullOrEmpty(body) ? null : body;
            if (Message is not null || authorName is not null)
                AuthorName = TextRules.RequiredName(authorName, nameof(authorName), "نام فرستنده");
        }
    }

    public int WorkItemId { get; private set; }
    public WorkItem WorkItem { get; private set; } = null!;
    public WorkItemActivityKind Kind { get; private set; }
    public string ActorUserId { get; private set; } = string.Empty;
    public string? AuthorName { get; private set; }
    public string? Message { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
}
