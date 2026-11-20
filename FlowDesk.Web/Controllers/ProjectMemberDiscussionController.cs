using FlowDesk.Application.Services;
using FlowDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.Web.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProjectMemberDiscussionController(
    ProjectMemberDiscussionService discussions, UserManager<ApplicationUser> accounts) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int projectId, int membershipId, int page = 1)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0) return BadRequest();
        var result = await discussions.ReadAsync(projectId, membershipId, page);
        return result is null ? NotFound() : View(result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add([FromForm] int projectId, [FromForm] int membershipId, [FromForm] string? comment)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0) return BadRequest();
        var account = await accounts.GetUserAsync(User);
        if (account is null) return Forbid();

        try
        {
            await discussions.AddAsync(projectId, membershipId,
                string.IsNullOrWhiteSpace(account.FullName) ? "عضو پروژه" : account.FullName, comment);
            TempData["SuccessMessage"] = "دیدگاه ثبت شد.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { projectId, membershipId });
    }
}
