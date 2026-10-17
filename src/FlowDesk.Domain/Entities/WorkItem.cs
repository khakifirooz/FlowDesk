using FlowDesk.Domain.Common;
using FlowDesk.Domain.Enums;

namespace FlowDesk.Domain.Entities
{
    public class WorkItem : BaseEntity
    {
        public string Title { get; private set; }
        public WorkItemStatus Status { get; private set; }
        public int ProjectId { get; private set; }

        public WorkItem(string title, int projectId)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException("title is required", nameof(title));
            }
            if (projectId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(projectId),"ProjectId must be greater than zero.");
            }

            Title = title;
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
    }
}
