using Microsoft.AspNetCore.Identity;

namespace FlowDesk.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Bio { get; set; }
}