using System.ComponentModel.DataAnnotations;
using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; private set; }

    [MaxLength(450)]
    public string? ManagerUserId { get; private set; }

    public Project(string name)
    {
        Name = TextRules.RequiredName(name, nameof(name), "نام پروژه");
    }

    public static Project CreateManaged(string name, string managerUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(managerUserId);

        if (managerUserId.Length > 450)
        {
            throw new ArgumentException(
                "شناسهٔ مدیر پروژه معتبر نیست.", nameof(managerUserId));
        }

        return new Project(name) { ManagerUserId = managerUserId };
    }

    public void Rename(string name)
    {
        Name = TextRules.RequiredName(name, nameof(name), "نام پروژه");
    }
}