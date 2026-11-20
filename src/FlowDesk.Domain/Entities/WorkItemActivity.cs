using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities;

public enum WorkItemActivityKind
{
    Created, Edited, Reassigned, Started, Completed, Reopened, Archived, Restored, Comment
}

public sealed class WorkItemActivity : BaseEntity
{
    private WorkItemActivity() { }

    public WorkItemActivity(WorkItem workItem, WorkItemActivityKind kind,
        string actorUserId, DateTimeOffset occurredAtUtc, string? comment = null, string? authorName = null)
    {
        WorkItem = workItem;
        Kind = kind;
        ActorUserId = actorUserId;
        OccurredAtUtc = occurredAtUtc;
        if (kind == WorkItemActivityKind.Comment)
        {
            var body = comment?.Trim();
            if (string.IsNullOrWhiteSpace(body) || body.Length > 1000)
                throw new ArgumentException("متن نظر باید بین ۱ تا ۱۰۰۰ کاراکتر باشد.", nameof(comment));
            Message = body;
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
