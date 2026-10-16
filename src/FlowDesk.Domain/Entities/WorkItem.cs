using FlowDesk.Domain.Enums;

namespace FlowDesk.Domain.Entities
{
    public class WorkItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public WorkItemStatus Status { get; set; }

        public WorkItem(string title)
        {
            Title = title;
            Status = WorkItemStatus.ToDo;
        }

        public void Start()
        {
            Status = WorkItemStatus.InProgress;
        }

        public void Complete()
        {
            Status = WorkItemStatus.Done;
        }

        public void Reopen()
        {
            Status = WorkItemStatus.ToDo;
        }
    }
}
