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
    UserManager<ApplicationUser> accounts, SignInManager<ApplicationUser> signIn) : Controller
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
            return View(new ManageInvitationModel(target with { HasActiveInvite = true, InvitedEmail = email, Version = issued.Version }, email, link));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(new ManageInvitationModel(target, email));
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke([FromForm] int projectId, [FromForm] int membershipId, [FromForm] Guid version)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0 || version == Guid.Empty)
            return BadRequest();
        try
        {
            await invitations.RevokeAsync(projectId, membershipId, version);
            TempData["SuccessMessage"] = "لینک دعوت غیرفعال شد.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Create), new { projectId, membershipId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve([FromForm] int projectId, [FromForm] int membershipId, [FromForm] Guid version)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0 || version == Guid.Empty)
            return BadRequest();

        try
        {
            var target = await invitations.GetTargetAsync(projectId, membershipId);
            if (target is null) return NotFound();
            var account = target.PendingUserId is string pendingUserId
                ? await accounts.FindByIdAsync(pendingUserId) : null;
            if (account is null)
                throw new InvalidOperationException("درخواست فعالی برای تأیید پیدا نشد.");

            await invitations.ApproveAsync(projectId, membershipId, account.Id, accounts.NormalizeEmail(account.Email), version);
            TempData["SuccessMessage"] = "درخواست تأیید شد و عضو به پروژه دسترسی پیدا کرد.";
            return RedirectToAction("Index", "ProjectMembers", new { projectId });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Create), new { projectId, membershipId });
        }
    }

    [HttpGet]
    public Task<IActionResult> Accept(string? token) => OpenAsync(token, accept: false);

    [HttpPost, ValidateAntiForgeryToken, ActionName(nameof(Accept))]
    public Task<IActionResult> AcceptPost([FromForm] string? token) => OpenAsync(token, accept: true);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchAccount([FromForm] string? token)
    {
        if (!ModelState.IsValid || token is null || token.Length != 64 || !token.All(Uri.IsHexDigit))
            return BadRequest();

        var account = await accounts.GetUserAsync(User);
        if (account is null) return Forbid();
        var outcome = await invitations.OpenAsync(token, account.Id, accounts.NormalizeEmail(account.Email));
        if (outcome.State != InvitationState.WrongAccount)
            return RedirectToAction(nameof(Accept), new { token });

        var returnUrl = Url.Action(nameof(Accept), "ProjectInvitations", new { token });
        await signIn.SignOutAsync();
        return RedirectToAction("Login", "Account", new { returnUrl });
    }

    private async Task<IActionResult> OpenAsync(string? token, bool accept)
    {
        var account = await accounts.GetUserAsync(User);
        if (account is null) return Forbid();

        try
        {
            var outcome = await invitations.OpenAsync(token, account.Id, accounts.NormalizeEmail(account.Email), accept);
            if (accept && outcome.State == InvitationState.PendingApproval)
                return RedirectToAction(nameof(Accept), new { token });

            return View(nameof(Accept), new AcceptInvitationModel(token, outcome));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nameof(Accept), new AcceptInvitationModel(null, new(InvitationState.Unavailable)));
        }
    }
}
