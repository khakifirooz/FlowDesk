using FlowDesk.Domain.Common;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Domain.Entities;

public class WorkItem : BaseEntity
{
    public string Title { get; private set; }

    public WorkItemStatus Status { get; private set; }

    public int ProjectId { get; private set; }

    public int? AssignedProjectMemberId { get; private set; }

    public WorkItem(string title, int projectId)
    {
        if (projectId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(projectId),
                "ProjectId must be greater than zero.");
        }

        Title = ValidateTitle(title);
        ProjectId = projectId;
        Status = WorkItemStatus.ToDo;
    }

    public void Start()
    {
        if (Status != WorkItemStatus.ToDo)
        {
            throw new InvalidOperationException(
                "Only a ToDo work item can be started.");
        }

        Status = WorkItemStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != WorkItemStatus.InProgress)
        {
            throw new InvalidOperationException(
                "Only an InProgress work item can be completed.");
        }

        Status = WorkItemStatus.Done;
    }

    public void Reopen()
    {
        if (Status != WorkItemStatus.Done)
        {
            throw new InvalidOperationException(
                "Only a Done work item can be reopened.");
        }

        Status = WorkItemStatus.ToDo;
    }

    public void ChangeTitle(string title)
    {
        Title = ValidateTitle(title);
    }

    public void AssignTo(ProjectMember projectMember)
    {
        ArgumentNullException.ThrowIfNull(projectMember);

        if (projectMember.Id <= 0)
        {
            throw new ArgumentException(
                "Project membership must be saved before assignment.",
                nameof(projectMember));
        }

        if (projectMember.ProjectId != ProjectId)
        {
            throw new InvalidOperationException(
                "The assignee must belong to the same project.");
        }

        AssignedProjectMemberId = projectMember.Id;
    }

    public void Unassign()
    {
        AssignedProjectMemberId = null;
    }

    private static string ValidateTitle(string title)
    {
        return TextRules.RequiredName(
            title,
            nameof(title),
            "عنوان تسک");
    }
}