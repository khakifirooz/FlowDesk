namespace FlowDesk.Application.DTOs.ProjectMembers;

public sealed record MemberCommentDto(string AuthorName, string Body, DateTimeOffset CreatedAtUtc);

public sealed record MemberDiscussionDto(int ProjectId, int MembershipId, string MemberName,
    IReadOnlyList<MemberCommentDto> Comments, int Page, int TotalPages);
