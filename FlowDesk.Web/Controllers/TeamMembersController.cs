using FlowDesk.Application.Common;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.TeamMembers;
using FlowDesk.Web.ViewModels.TeamMembers;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.Web.Controllers;

public class TeamMembersController : Controller
{
    private readonly ITeamMemberService _teamMemberService;
    private readonly ProjectAccess _projectAccess;

    public TeamMembersController(
        ITeamMemberService teamMemberService, ProjectAccess projectAccess)
    {
        _teamMemberService = teamMemberService;
        _projectAccess = projectAccess;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var members = await _teamMemberService.GetAllAsync();

        var model = members
            .Select(member => new TeamMemberListItemViewModel
            {
                Id = member.Id,
                FullName = member.FullName,
                CanEdit = member.CanEdit,
                CanDelete = member.CanDelete
            })
            .ToList();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? projectId)
    {
        if (!ModelState.IsValid || projectId is <= 0)
            return BadRequest();
        var scope = await _projectAccess.GetScopeAsync();
        scope.RequireTeamDirectory();
        if (projectId is int id)
            scope.RequireManager(id);

        return View(new CreateTeamMemberViewModel { ProjectId = projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateTeamMemberViewModel model)
    {
        var scope = await _projectAccess.GetScopeAsync();
        scope.RequireTeamDirectory();
        if (model.ProjectId is int requestedProject)
            scope.RequireManager(requestedProject);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var input = new CreateTeamMemberDto
            {
                FullName = model.FullName
            };

            int id = await _teamMemberService.CreateAsync(input);

            if (model.ProjectId is int projectId)
            {
                TempData["SuccessMessage"] = "فرد ثبت شد. او را از فهرست انتخاب و به پروژه اضافه کنید.";
                return RedirectToAction("Index", "ProjectMembers", new { projectId });
            }

            TempData["SuccessMessage"] =
                $"عضو جدید با شناسهٔ {id} ثبت شد.";

            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException)
        {
            ModelState.AddModelError(
                nameof(model.FullName),
                "نام عضو معتبر نیست.");

            return View(model);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(
                nameof(model.FullName),
                exception.Message);

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!ModelState.IsValid || id <= 0)
        {
            return BadRequest("شناسهٔ عضو معتبر نیست.");
        }

        var member = await _teamMemberService.GetByIdAsync(id);

        if (member is null)
        {
            return NotFound("عضو پیدا نشد.");
        }

        var model = new EditTeamMemberViewModel
        {
            Id = member.Id,
            FullName = member.FullName
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        EditTeamMemberViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _teamMemberService.RenameAsync(
                model.Id,
                model.FullName);

            TempData["SuccessMessage"] = "نام عضو ویرایش شد.";

            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound("عضو پیدا نشد.");
        }
        catch (ArgumentException)
        {
            ModelState.AddModelError(
                nameof(model.FullName),
                "نام عضو معتبر نیست.");

            return View(model);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(
                nameof(model.FullName),
                exception.Message);

            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!ModelState.IsValid || id <= 0)
        {
            return BadRequest("شناسهٔ عضو معتبر نیست.");
        }

        try
        {
            await _teamMemberService.DeleteAsync(id);

            TempData["SuccessMessage"] = "عضو از سامانه حذف شد.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound("عضو پیدا نشد.");
        }
        catch (InvalidOperationException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}