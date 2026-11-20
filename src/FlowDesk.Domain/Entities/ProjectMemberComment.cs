using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities;

public sealed class ProjectMemberComment : BaseEntity
{
    public const int BodyMaxLength = 1000;
    private ProjectMemberComment() { }

    public ProjectMemberComment(int projectId, int subjectMemberId, string authorUserId, string authorName,
        string? body, DateTimeOffset createdAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(projectId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(subjectMemberId);
        ArgumentException.ThrowIfNullOrWhiteSpace(authorUserId);
        var text = body?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > BodyMaxLength)
            throw new ArgumentException("دیدگاه باید بین ۱ تا ۱۰۰۰ کاراکتر باشد.", nameof(body));

        ProjectId = projectId;
        SubjectMemberId = subjectMemberId;
        AuthorUserId = authorUserId;
        AuthorName = TextRules.RequiredName(authorName, nameof(authorName), "نام نویسنده");
        Body = text;
        CreatedAtUtc = createdAtUtc;
    }

    public int ProjectId { get; private set; }
    public int SubjectMemberId { get; private set; }
    public string AuthorUserId { get; private set; } = string.Empty;
    public string AuthorName { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
