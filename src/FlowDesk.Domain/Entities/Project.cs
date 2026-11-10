using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; private set; }

    public Project(string name)
    {
        Name = TextRules.RequiredName(
            name,
            nameof(name),
            "نام پروژه");
    }

    public void Rename(string name)
    {
        Name = TextRules.RequiredName(
            name,
            nameof(name),
            "نام پروژه");
    }
} 