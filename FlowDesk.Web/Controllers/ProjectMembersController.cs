using System.ComponentModel.DataAnnotations;
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

namespace FlowDesk.Web.Controllers;

[Authorize]
public class ProjectMembersController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ITeamMemberService _teamMemberService;
    private readonly IProjectMemberService _projectMemberService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ProjectAccess _access;

    public ProjectMembersController(
        IProjectService projectService,
        ITeamMemberService teamMemberService,
        IProjectMemberService projectMemberService,
        UserManager<ApplicationUser> userManager,
        ProjectAccess access)
    {
        _projectService = projectService;
        _teamMemberService = teamMemberService;
        _projectMemberService = projectMemberService;
        _userManager = userManager;
        _access = access;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> LinkAccount(
        [FromForm] int projectId,
        [FromForm] int membershipId,
        [FromForm] string? email)
    {
        return ChangeAccountAsync(projectId, membershipId, email, unlink: false);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> UnlinkAccount(
        [FromForm] int projectId,
        [FromForm] int membershipId)
    {
        return ChangeAccountAsync(projectId, membershipId, email: null, unlink: true);
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

    private async Task<IActionResult> ChangeAccountAsync(
        int projectId,
        int membershipId,
        string? email,
        bool unlink)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0)
            return BadRequest();

        var project = await _projectService.GetByIdAsync(projectId);

        if (project is null)
            return NotFound();

        if (!CanManage(project))
            return Forbid();

        string? accountUserId = null;

        if (!unlink)
        {
            email = email?.Trim();

            if (string.IsNullOrWhiteSpace(email) || email.Length > 256 ||
                !new EmailAddressAttribute().IsValid(email))
            {
                TempData["ErrorMessage"] = "یک ایمیل معتبر وارد کنید.";
                return RedirectToAction(nameof(Index), new { projectId });
            }

            var account = await _userManager.FindByEmailAsync(email);

            if (account is null)
            {
                TempData["ErrorMessage"] = "با این ایمیل حسابی ثبت نشده است.";
                return RedirectToAction(nameof(Index), new { projectId });
            }

            accountUserId = account.Id;
        }

        try
        {
            await _projectMemberService.SetAccountAsync(
                project.Id,
                membershipId,
                accountUserId);

            TempData["SuccessMessage"] = unlink
                ? "اتصال حساب از این عضویت برداشته شد."
                : "حساب به این عضو پروژه متصل شد.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

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