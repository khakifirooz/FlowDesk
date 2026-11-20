using FlowDesk.Domain.Common;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Domain.Entities;

public class WorkItem : BaseEntity
{
    public string Title { get; private set; }
    public WorkItemStatus Status { get; private set; }
    public int ProjectId { get; private set; }
    public int? AssignedProjectMemberId { get; private set; }
    public string? Description { get; private set; }
    public WorkItemPriority Priority { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public DateTimeOffset? CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? ArchivedAtUtc { get; private set; }
    public Guid? Revision { get; private set; }

    public WorkItem(string title, int projectId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(projectId);
        Title = ValidateTitle(title);
        ProjectId = projectId;
        Status = WorkItemStatus.ToDo;
    }

    public void RecordCreation(DateTimeOffset now)
    {
        CreatedAtUtc ??= now;
    }

    public void Start()
    {
        RequireActive();
        if (Status != WorkItemStatus.ToDo)
            throw new InvalidOperationException("فقط تسک شروع‌نشده را می‌توانید شروع کنید.");
        Status = WorkItemStatus.InProgress;
        Touch();
    }

    public void Complete() => Complete(DateTimeOffset.UtcNow);

    public void Complete(DateTimeOffset now)
    {
        RequireActive();
        if (Status != WorkItemStatus.InProgress)
            throw new InvalidOperationException("فقط تسک در حال انجام را می‌توانید تکمیل کنید.");
        MarkDone(now);
    }

    public void ConfirmCompletion(DateTimeOffset now)
    {
        RequireActive();
        if (Status is not (WorkItemStatus.ToDo or WorkItemStatus.InProgress))
            throw new InvalidOperationException("فقط تسک باز را می‌توانید تکمیل کنید.");
        MarkDone(now);
    }

    private void MarkDone(DateTimeOffset now)
    {
        Status = WorkItemStatus.Done;
        CompletedAtUtc = now;
        Touch();
    }

    public void Reopen()
    {
        RequireActive();
        if (Status != WorkItemStatus.Done)
            throw new InvalidOperationException("فقط تسک تکمیل‌شده را می‌توانید بازگشایی کنید.");
        Status = WorkItemStatus.ToDo;
        CompletedAtUtc = null;
        Touch();
    }

    public void ChangeTitle(string title)
    {
        RequireActive();
        Title = ValidateTitle(title);
        Touch();
    }

    public void ChangeDetails(string title, string? description, WorkItemPriority priority, DateOnly? dueDate)
    {
        RequireActive();
        if (!Enum.IsDefined(priority))
            throw new ArgumentException("اولویت معتبر نیست.", nameof(priority));
        var cleanTitle = ValidateTitle(title);
        var cleanDescription = description?.Trim();
        if (cleanDescription?.Length > 2000)
            throw new ArgumentException("شرح تسک حداکثر ۲۰۰۰ کاراکتر است.", nameof(description));
        Title = cleanTitle;
        Description = string.IsNullOrEmpty(cleanDescription) ? null : cleanDescription;
        Priority = priority;
        DueDate = dueDate;
        Touch();
    }

    public void AssignTo(ProjectMember projectMember)
    {
        RequireActive();
        ArgumentNullException.ThrowIfNull(projectMember);
        if (projectMember.Id <= 0)
            throw new ArgumentException("ابتدا عضویت مسئول باید ثبت شود.", nameof(projectMember));
        if (projectMember.ProjectId != ProjectId)
            throw new InvalidOperationException("مسئول باید عضو همان پروژه باشد.");
        AssignedProjectMemberId = projectMember.Id;
        Touch();
    }

    public void Unassign()
    {
        RequireActive();
        AssignedProjectMemberId = null;
        Touch();
    }

    public void Archive(DateTimeOffset now)
    {
        RequireActive();
        ArchivedAtUtc = now;
        Touch();
    }

    public void Restore()
    {
        if (ArchivedAtUtc is null)
            throw new InvalidOperationException("این تسک در بایگانی نیست.");
        ArchivedAtUtc = null;
        Touch();
    }

    public void ReviewRecorded()
    {
        RequireActive();
        if (Status != WorkItemStatus.Done)
            throw new InvalidOperationException("فقط تسک انجام‌شده قابل ارزیابی است.");
        Touch();
    }

    public void CommentAdded()
    {
        RequireActive();
        Touch();
    }

    private void RequireActive()
    {
        if (ArchivedAtUtc is not null)
            throw new InvalidOperationException("برای تغییر این تسک، ابتدا آن را از بایگانی برگردانید.");
    }

    private void Touch() => Revision = Guid.NewGuid();

    private static string ValidateTitle(string title) =>
        TextRules.RequiredName(title, nameof(title), "عنوان تسک");
}
