using System.Security.Claims;
using FlowDesk.Application.Common;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.Projects;
using FlowDesk.Infrastructure.Identity;
using FlowDesk.Web.ViewModels.ProjectMembers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FlowDesk.Web.Controllers;

[Authorize]
public class ProjectMembersController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ITeamMemberService _teamMemberService;
    private readonly IProjectMemberService _projectMemberService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ProjectAccess _access;
    private readonly ProjectInvitationService _invitations;

    public ProjectMembersController(
        IProjectService projectService,
        ITeamMemberService teamMemberService,
        IProjectMemberService projectMemberService,
        UserManager<ApplicationUser> userManager,
        ProjectAccess access,
        ProjectInvitationService invitations)
    {
        _projectService = projectService;
        _teamMemberService = teamMemberService;
        _projectMemberService = projectMemberService;
        _userManager = userManager;
        _access = access;
        _invitations = invitations;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int projectId)
    {
        if (!ModelState.IsValid || projectId <= 0)
            return BadRequest();

        var project = await _projectService.GetByIdAsync(projectId);

        if (project is null)
            return NotFound();

        return View(await BuildModelAsync(project));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(ProjectMembersViewModel model)
    {
        if (model.ProjectId <= 0)
            return BadRequest();

        var project = await _projectService.GetByIdAsync(model.ProjectId);

        if (project is null)
            return NotFound();

        if (!CanManage(project))
            return Forbid();

        if (!ModelState.IsValid)
            return View(nameof(Index), await BuildModelAsync(project, model.TeamMemberId));

        try
        {
            await _projectMemberService.AddAsync(project.Id, model.TeamMemberId);

            TempData["SuccessMessage"] = "عضو به پروژه اضافه شد.";
            return RedirectToAction(nameof(Index), new { projectId = project.Id });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(model.TeamMemberId), exception.Message);
            return View(nameof(Index), await BuildModelAsync(project, model.TeamMemberId));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int projectId, int membershipId)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0)
            return BadRequest();

        var project = await _projectService.GetByIdAsync(projectId);

        if (project is null)
            return NotFound();

        if (!CanManage(project))
            return Forbid();

        try
        {
            await _projectMemberService.RemoveAsync(project.Id, membershipId);
            TempData["SuccessMessage"] = "عضو از این پروژه حذف شد.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { projectId = project.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkAccount([FromForm] int projectId, [FromForm] int membershipId)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0)
            return BadRequest();

        (await _access.GetScopeAsync()).RequireManager(projectId);
        try
        {
            await _projectMemberService.SetAccountAsync(projectId, membershipId, accountUserId: null);
            TempData["SuccessMessage"] = "اتصال حساب از این عضویت برداشته شد.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkMyAccount([FromForm] int projectId, [FromForm] int membershipId)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0) return BadRequest();
        var scope = await _access.GetScopeAsync();
        scope.RequireManager(projectId);
        var account = await _userManager.GetUserAsync(User);
        if (account is null) return Forbid();

        try
        {
            await _projectMemberService.SetAccountAsync(projectId, membershipId, account.Id);
            TempData["SuccessMessage"] = "حساب شما به این ردیف عضویت متصل شد.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }

        return RedirectToAction(nameof(Index), new { projectId });
    }

    private bool CanManage(ProjectListItemDto project)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return User.Identity?.IsAuthenticated == true &&
            !string.IsNullOrWhiteSpace(userId) &&
            string.Equals(project.ManagerUserId, userId, StringComparison.Ordinal);
    }

    private async Task<ProjectMembersViewModel> BuildModelAsync(
        ProjectListItemDto project,
        int selectedMemberId = 0)
    {
        var memberships = await _projectMemberService.GetByProjectIdAsync(project.Id);
        bool canManage = CanManage(project);

        var model = new ProjectMembersViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            CanManageMembers = canManage,
            TeamMemberId = selectedMemberId,
            Members = memberships
        };

        if (canManage)
        {
            var scope = await _access.GetScopeAsync();
            model.OwnMembershipIds = scope.OwnMembershipIds.ToHashSet();
            var accountIds = memberships.Select(x => x.LinkedAccountUserId).OfType<string>().Distinct().ToList();
            if (accountIds.Count > 0)
            {
                var emails = await _userManager.Users.AsNoTracking()
                    .Where(account => accountIds.Contains(account.Id))
                    .ToDictionaryAsync(account => account.Id, account => account.Email);
                foreach (var membership in memberships)
                {
                    if (membership.LinkedAccountUserId is string uid &&
                        emails.TryGetValue(uid, out var email) && !string.IsNullOrWhiteSpace(email))
                        model.MemberEmails[membership.Id] = email;
                }
            }
            model.InvitationStatus = (await _invitations.GetManagerInvitesAsync(project.Id)).ToDictionary(x => x.MembershipId);
            var existingIds = memberships.Select(x => x.TeamMemberId).ToHashSet();
            var allMembers = await _teamMemberService.GetAllAsync();

            model.AvailableMembers = allMembers
                .Where(x => !existingIds.Contains(x.Id))
                .OrderBy(x => x.FullName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.FullName
                })
                .ToList();
        }

        return model;
    }
}