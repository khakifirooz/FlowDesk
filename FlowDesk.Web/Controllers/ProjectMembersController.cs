using FlowDesk.Application.Contracts.Services;
using FlowDesk.Web.ViewModels.ProjectMembers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FlowDesk.Web.Controllers;

public class ProjectMembersController : Controller
{
    private readonly IProjectService _projectService;
    private readonly ITeamMemberService _teamMemberService;
    private readonly IProjectMemberService _projectMemberService;

    public ProjectMembersController(
        IProjectService projectService,
        ITeamMemberService teamMemberService,
        IProjectMemberService projectMemberService)
    {
        _projectService = projectService;
        _teamMemberService = teamMemberService;
        _projectMemberService = projectMemberService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int projectId)
    {
        if (!ModelState.IsValid || projectId <= 0)
            return BadRequest();

        var model = await BuildModelAsync(projectId);

        if (model is null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(ProjectMembersViewModel model)
    {
        if (model.ProjectId <= 0)
            return BadRequest();

        if (!ModelState.IsValid)
            return await ShowIndexAsync(model);

        try
        {
            await _projectMemberService.AddAsync(
                model.ProjectId,
                model.TeamMemberId);

            TempData["SuccessMessage"] = "عضو به پروژه اضافه شد.";

            return RedirectToAction(
                nameof(Index),
                new { projectId = model.ProjectId });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(
                nameof(model.TeamMemberId),
                exception.Message);

            return await ShowIndexAsync(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int projectId, int membershipId)
    {
        if (!ModelState.IsValid || projectId <= 0 || membershipId <= 0)
            return BadRequest();

        try
        {
            await _projectMemberService.RemoveAsync(
                projectId,
                membershipId);

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

        return RedirectToAction(nameof(Index), new { projectId });
    }

    private async Task<IActionResult> ShowIndexAsync(
        ProjectMembersViewModel input)
    {
        var model = await BuildModelAsync(
            input.ProjectId,
            input.TeamMemberId);

        if (model is null)
            return NotFound();

        return View("Index", model);
    }

    private async Task<ProjectMembersViewModel?> BuildModelAsync(
        int projectId,
        int selectedMemberId = 0)
    {
        var project = await _projectService.GetByIdAsync(projectId);

        if (project is null)
            return null;

        var memberships = await _projectMemberService
            .GetByProjectIdAsync(projectId);

        var allMembers = await _teamMemberService.GetAllAsync();

        var existingMemberIds = memberships
            .Select(x => x.TeamMemberId)
            .ToHashSet();

        var availableMembers = allMembers
            .Where(x => !existingMemberIds.Contains(x.Id))
            .OrderBy(x => x.FullName)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.FullName
            })
            .ToList();

        return new ProjectMembersViewModel
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            TeamMemberId = selectedMemberId,
            Members = memberships,
            AvailableMembers = availableMembers
        };
    }
}