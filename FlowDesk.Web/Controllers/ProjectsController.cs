using System.Security.Claims;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.Projects;
using FlowDesk.Web.ViewModels.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.Web.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetAllAsync();

        var model = projects
            .OrderByDescending(project => project.Id)
            .Select(project => new ProjectListItemViewModel
            {
                CanManage = project.ManagerUserId == User.FindFirstValue(ClaimTypes.NameIdentifier),
                Id = project.Id,
                Name = project.Name
            })
            .ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateProjectViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateProjectViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        try
        {
            var input = new CreateProjectDto
            {
                Name = model.Name,
                ManagerUserId = userId
            };

            int projectId = await _projectService.CreateAsync(input);

            TempData["SuccessMessage"] =
                "پروژه ساخته شد. حالا می‌توانید اعضای تیم را اضافه کنید.";

            return RedirectToAction("Index", "ProjectMembers", new { projectId });
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                exception.Message);

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!ModelState.IsValid || id <= 0)
        {
            return BadRequest("شناسهٔ پروژه معتبر نیست.");
        }

        var project = await _projectService.GetByIdAsync(id);

        if (project is null)
        {
            return NotFound("پروژه پیدا نشد.");
        }

        if (project.ManagerUserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            return Forbid();

        var model = new EditProjectViewModel
        {
            Id = project.Id,
            Name = project.Name
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditProjectViewModel model)
    {
        if (id <= 0 || id != model.Id)
        {
            return BadRequest("شناسهٔ پروژه معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _projectService.RenameAsync(
                id,
                model.Name);

            TempData["SuccessMessage"] = "نام پروژه ویرایش شد.";

            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound("پروژه پیدا نشد.");
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                exception.Message);

            return View(model);
        }
    }
}