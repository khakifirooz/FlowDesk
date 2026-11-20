using FlowDesk.Application.Common;
using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.DTOs.ProjectMembers;
using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Services;

public sealed class ProjectMemberDiscussionService(
    IRepository<ProjectMemberComment> comments, IRepository<ProjectMember> memberships,
    IRepository<TeamMember> people, ProjectAccess access, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<MemberDiscussionDto?> ReadAsync(int projectId, int membershipId, int page)
    {
        var scope = await access.GetViewScopeAsync();
        if (!scope.VisibleProjectIds.Contains(projectId)) return null;
        var subject = await FindSubjectAsync(projectId, membershipId);
        if (subject is null) return null;

        const int pageSize = 20;
        var count = await comments.CountAsync(x => x.ProjectId == projectId && x.SubjectMemberId == membershipId);
        var pages = Math.Max(1, (int)Math.Ceiling(count / (double)pageSize));
        var selectedPage = Math.Clamp(page, 1, pages);
        var rows = await comments.GetPageAsync(x => x.ProjectId == projectId && x.SubjectMemberId == membershipId,
            selectedPage, pageSize);

        return new(projectId, membershipId, subject.FullName,
            rows.Select(x => new MemberCommentDto(x.AuthorName, x.Body, x.CreatedAtUtc)).ToArray(), selectedPage, pages);
    }

    public async Task AddAsync(int projectId, int membershipId, string authorName, string? text)
    {
        var scope = await access.GetScopeAsync();
        if (!scope.VisibleProjectIds.Contains(projectId) || await FindSubjectAsync(projectId, membershipId) is null)
            throw new KeyNotFoundException("عضو پروژه پیدا نشد.");

        var comment = new ProjectMemberComment(projectId, membershipId, scope.UserId, authorName, text, clock.GetUtcNow());

        if (!scope.CanManage(projectId))
        {
            var links = await memberships.FindAsync(x => x.ProjectId == projectId && x.AccountUserId == scope.UserId);
            var membership = links.SingleOrDefault();
            if (membership is null || !string.Equals(membership.AccountUserId, scope.UserId, StringComparison.Ordinal))
                throw new UnauthorizedAccessException("برای ثبت دیدگاه باید عضو این پروژه باشید.");

            unitOfWork.VerifyMembershipOnSave(membership);
        }

        await comments.AddAsync(comment);
        await unitOfWork.SaveChangesAsync();
    }

    private async Task<TeamMember?> FindSubjectAsync(int projectId, int membershipId)
    {
        var link = await memberships.GetByIdAsync(membershipId);
        return link?.ProjectId == projectId ? await people.GetByIdAsync(link.TeamMemberId) : null;
    }
}
