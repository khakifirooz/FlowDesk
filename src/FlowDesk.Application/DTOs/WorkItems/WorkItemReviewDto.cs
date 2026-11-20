namespace FlowDesk.Application.DTOs.WorkItems;

public sealed record WorkItemReviewDto(string RecipientName, int? Score, string? Feedback,
    string ReviewerName, DateTimeOffset ReviewedAtUtc, Guid Revision);

public sealed record WorkItemReviewPage(int? SubmissionId, string? RecipientName,
    bool CanSave, WorkItemReviewDto? Review, string? UnavailableMessage);

public sealed record SaveWorkItemReview(int SubmissionId, int? Score, string? Feedback, Guid? Revision);
