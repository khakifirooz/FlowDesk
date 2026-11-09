using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.Projects;
using FlowDesk.Web.ViewModels.Projects;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.Web.Controllers;

public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetAllAsync();

        var model = new List<ProjectListItemViewModel>();

        foreach (var project in projects)
        {
            model.Add(new ProjectListItemViewModel
            {
                Id = project.Id,
                Name = project.Name
            });
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateProjectViewModel());
    }

    [HttpGet]
    

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateProjectViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var input = new CreateProjectDto
        {
            Name = model.Name
        };

        int projectId = await _projectService.CreateAsync(input);

        TempData["SuccessMessage"] =
            $"پروژه با شناسهٔ {projectId} ثبت شد.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projectService.GetByIdAsync(id);

        if (project is null)
        {
            return NotFound("پروژه پیدا نشد.");
        }

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
        if (id != model.Id)
        {
            return BadRequest("شناسهٔ پروژه معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _projectService.RenameAsync(id, model.Name);

            TempData["SuccessMessage"] = "نام پروژه ویرایش شد.";

            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound("پروژه پیدا نشد.");
        }
        catch (ArgumentException)
        {
            ModelState.AddModelError(
                nameof(model.Name), "نام پروژه معتبر نیست.");

            return View(model);
        }
    }


}