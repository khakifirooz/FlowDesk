using System.ComponentModel.DataAnnotations;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FlowDesk.Web.Controllers;

public sealed record ManageInvitationModel(InvitationTarget Target, string? Email = null, string? Link = null);
public sealed record AcceptInvitationModel(string? Token, InvitationOutcome Outcome);

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProjectInvitationsController(
    ProjectInvitationService invitations,
    UserManager<ApplicationUser> accounts) : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        base.OnActionExecuting(context);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int projectId, int membershipId)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0) return BadRequest();
        var target = await invitations.GetTargetAsync(projectId, membershipId);
        return target is null ? NotFound() : View(new ManageInvitationModel(target));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromForm] int projectId, [FromForm] int membershipId, [FromForm] string? email)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0) return BadRequest();
        var target = await invitations.GetTargetAsync(projectId, membershipId);
        if (target is null) return NotFound();

        email = email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || email.Length > 256 ||
            !new EmailAddressAttribute().IsValid(email))
        {
            ModelState.AddModelError(string.Empty, "ایمیل معتبر عضو را وارد کنید.");
            return View(new ManageInvitationModel(target, email));
        }

        try
        {
            var issued = await invitations.IssueAsync(projectId, membershipId, accounts.NormalizeEmail(email)!);
            var link = Url.Action(nameof(Accept), "ProjectInvitations", new { token = issued.Token }, Request.Scheme);
            return View(new ManageInvitationModel(target with { HasActiveInvite = true }, email, link));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(new ManageInvitationModel(target, email));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke([FromForm] int projectId, [FromForm] int membershipId)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0) return BadRequest();
        try
        {
            await invitations.RevokeAsync(projectId, membershipId);
            TempData["SuccessMessage"] = "لینک دعوت غیرفعال شد.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Create), new { projectId, membershipId });
    }

    [HttpGet]
    public Task<IActionResult> Accept(string? token) => OpenAsync(token, accept: false);

    [HttpPost, ValidateAntiForgeryToken, ActionName(nameof(Accept))]
    public Task<IActionResult> AcceptPost([FromForm] string? token) => OpenAsync(token, accept: true);

    private async Task<IActionResult> OpenAsync(string? token, bool accept)
    {
        var account = await accounts.GetUserAsync(User);
        if (account is null) return Forbid();

        try
        {
            var outcome = await invitations.OpenAsync(token, account.Id, accounts.NormalizeEmail(account.Email), accept);
            if (outcome.State == InvitationState.Accepted)
            {
                TempData["SuccessMessage"] = "دعوت پذیرفته شد و به پروژه اضافه شدید.";
                return RedirectToAction("Index", "ProjectMembers", new { projectId = outcome.ProjectId });
            }
            return View(nameof(Accept), new AcceptInvitationModel(token, outcome));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nameof(Accept), new AcceptInvitationModel(null, new(InvitationState.Unavailable)));
        }
    }
}
