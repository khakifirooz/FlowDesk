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

        var input = new CreateProjectDto
        {
            Name = model.Name
        };

        int projectId = await _projectService.CreateAsync(input);

        TempData["SuccessMessage"] =
            $"پروژه با شناسهٔ {projectId} ثبت شد.";

        return RedirectToAction(nameof(Create));
    }
}