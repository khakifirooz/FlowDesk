using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities
{
    public class Project : BaseEntity
    {
        public string Name { get; private set; }

        public Project(string name)
        {
            Name = ValidateName(name);
        }

        public void Rename(string name)
        {
            Name = ValidateName(name);
        }
        private static string ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Project name is required.", nameof(name));
            }

            return name.Trim();
        }
    }
}
