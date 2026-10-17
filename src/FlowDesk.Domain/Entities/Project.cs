using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities
{
    public class Project : BaseEntity
    {
        public string Name { get; private set; }

        public Project(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Project name is required", nameof(name));
            }

            Name = name.Trim();
            /////////////
        }
    }
}
