using FlowDesk.Domain.Common;

namespace FlowDesk.Domain.Entities;

public class TeamMember : BaseEntity
{
    public string FullName { get; private set; }

    public TeamMember(string fullName)
    {
        FullName = ValidateFullName(fullName);
    }

    public void Rename(string fullName)
    {
        FullName = ValidateFullName(fullName);
    }

    //private static string ValidateFullName(string fullName)
    //{
    //    if (string.IsNullOrWhiteSpace(fullName))
    //    {
    //        throw new ArgumentException(
    //            "Member name is required.", nameof(fullName));
    //    }

    //    return fullName.Trim();
    //}

    private static string ValidateFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(
                "Member name is required.", nameof(fullName));
        }

        string trimmedName = fullName.Trim();

        if (trimmedName.Length > 200)
        {
            throw new ArgumentException(
                "Member name cannot exceed 200 characters.",
                nameof(fullName));
        }

        return trimmedName;
    }
}