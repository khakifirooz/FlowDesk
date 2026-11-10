using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.WorkItems;
using FlowDesk.Domain.Enums;
using FlowDesk.Web.ViewModels.WorkItems;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FlowDesk.Web.Controllers;

public class WorkItemsController : Controller
{
    private readonly IWorkItemService _workItemService;
    private readonly IProjectService _projectService;

    public WorkItemsController(
        IWorkItemService workItemService,
        IProjectService projectService)
    {
        _workItemService = workItemService;
        _projectService = projectService;
    }

    [HttpGet]
    [HttpGet]
    [HttpGet]
    public async Task<IActionResult> Index(
    WorkItemIndexViewModel model)
    {
        var projects = await _projectService.GetAllAsync();

        model.Projects = projects
            .OrderBy(project => project.Name)
            .Select(project => new SelectListItem
            {
                Value = project.Id.ToString(),
                Text = project.Name
            })
            .ToList();

        if (!ModelState.IsValid)
        {
            model.PageNumber = 1;
            return View(model);
        }

        var input = new WorkItemSearchDto
        {
            SearchText = model.SearchText,
            ProjectId = model.ProjectId,
            Status = model.Status,
            PageNumber = model.PageNumber
        };

        var result = await _workItemService.SearchAsync(input);

        model.TotalCount = result.TotalCount;
        model.PageNumber = result.PageNumber;
        model.PageSize = result.PageSize;

        var projectNames = projects.ToDictionary(
            project => project.Id,
            project => project.Name);

        model.Items = result.Items.Select(item =>
            new WorkItemListItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Status = item.Status,
                ProjectName = projectNames.TryGetValue(
                    item.ProjectId, out var name)
                        ? name
                        : "پروژه یافت نشد",
                StatusText = item.Status switch
                {
                    WorkItemStatus.ToDo => "انجام‌نشده",
                    WorkItemStatus.InProgress => "در حال انجام",
                    WorkItemStatus.Done => "انجام‌شده",
                    _ => "نامشخص"
                }
            }).ToList();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateWorkItemViewModel();

        await LoadProjectsAsync(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateWorkItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadProjectsAsync(model);
            return View(model);
        }

        try
        {
            var input = new CreateWorkItemDto
            {
                Title = model.Title,
                ProjectId = model.ProjectId
            };

            int id = await _workItemService.CreateAsync(input);

            TempData["SuccessMessage"] =
                $"تسک با شناسهٔ {id} ثبت شد.";

            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);

            await LoadProjectsAsync(model);

            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
    int id,
    WorkItemStatus targetStatus)
    {
        if (!ModelState.IsValid ||
            id <= 0 ||
            !Enum.IsDefined(typeof(WorkItemStatus), targetStatus))
        {
            return BadRequest("درخواست تغییر وضعیت معتبر نیست.");
        }

        try
        {
            await _workItemService.ChangeStatusAsync(id, targetStatus);

            TempData["SuccessMessage"] = "وضعیت تسک تغییر کرد.";
        }
        catch (KeyNotFoundException)
        {
            return NotFound("تسک موردنظر پیدا نشد.");
        }
        catch (InvalidOperationException)
        {
            TempData["ErrorMessage"] =
                "این تغییر وضعیت مجاز نیست. وضعیت فعلی تسک را بررسی کنید.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadProjectsAsync(
        CreateWorkItemViewModel model)
    {
        var projects = await _projectService.GetAllAsync();

        model.Projects = projects
            .OrderBy(project => project.Name)
            .Select(project => new SelectListItem
            {
                Value = project.Id.ToString(),
                Text = project.Name
            })
            .ToList();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var workItem = await _workItemService.GetByIdAsync(id);

        if (workItem is null)
        {
            return NotFound("تسک پیدا نشد.");
        }

        var model = new EditWorkItemViewModel
        {
            Id = workItem.Id,
            Title = workItem.Title
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditWorkItemViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest("شناسهٔ تسک معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _workItemService.ChangeTitleAsync(id, model.Title);

            TempData["SuccessMessage"] = "عنوان تسک ویرایش شد.";

            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound("تسک پیدا نشد.");
        }
        catch (ArgumentException)
        {
            ModelState.AddModelError(
                nameof(model.Title), "عنوان تسک معتبر نیست.");

            return View(model);
        }
    }
}