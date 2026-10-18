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
            // برای برداشتن فاصله و منظم کردن اون استرینگ trim
            Name = name.Trim();
        }
    }
}
