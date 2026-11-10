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
    private readonly IProjectMemberService _projectMemberService;

    public WorkItemsController(
        IWorkItemService workItemService,
        IProjectService projectService,
        IProjectMemberService projectMemberService)
    {
        _workItemService = workItemService;
        _projectService = projectService;
        _projectMemberService = projectMemberService;
    }

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

        model.Items = result.Items
            .Select(item => new WorkItemListItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Status = item.Status,
                AssignedProjectMemberId = item.AssignedProjectMemberId,

                ProjectName = projectNames.TryGetValue(
                    item.ProjectId,
                    out var name)
                        ? name
                        : "پروژه یافت نشد",

                StatusText = item.Status switch
                {
                    WorkItemStatus.ToDo => "انجام‌نشده",
                    WorkItemStatus.InProgress => "در حال انجام",
                    WorkItemStatus.Done => "انجام‌شده",
                    _ => "نامشخص"
                }
            })
            .ToList();

        var membershipIds = model.Items
            .Where(item => item.AssignedProjectMemberId.HasValue)
            .Select(item =>
                item.AssignedProjectMemberId.GetValueOrDefault())
            .Distinct()
            .ToList();

        var assigneeNames = await _projectMemberService
            .GetNamesByIdsAsync(membershipIds);

        foreach (var item in model.Items)
        {
            if (item.AssignedProjectMemberId is int membershipId)
            {
                item.AssignedMemberName =
                    assigneeNames.TryGetValue(
                        membershipId,
                        out var fullName)
                            ? fullName
                            : "عضویت یافت نشد";
            }
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? projectId)
    {
        if (!ModelState.IsValid || projectId is <= 0)
        {
            return BadRequest();
        }

        var model = new CreateWorkItemViewModel
        {
            ProjectId = projectId
        };

        if (!await LoadCreateOptionsAsync(model))
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateWorkItemViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var input = new CreateWorkItemDto
                {
                    Title = model.Title,
                    ProjectId = model.ProjectId!.Value,
                    AssignedProjectMemberId =
                        model.AssignedProjectMemberId!.Value
                };

                int id = await _workItemService.CreateAsync(input);

                TempData["SuccessMessage"] =
                    $"تسک با شناسهٔ {id} ثبت شد.";

                return RedirectToAction(
                    nameof(Index),
                    new { projectId = model.ProjectId });
            }
            catch (Exception exception) when (
                exception is ArgumentException or InvalidOperationException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    exception.Message);
            }
        }

        if (!await LoadCreateOptionsAsync(model))
        {
            ModelState.AddModelError(
                nameof(model.ProjectId),
                "پروژهٔ انتخاب‌شده وجود ندارد.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        int id,
        WorkItemStatus targetStatus,
        string? returnUrl)
    {
        if (!ModelState.IsValid ||
            id <= 0 ||
            !Enum.IsDefined(typeof(WorkItemStatus), targetStatus))
        {
            return BadRequest(
                "درخواست تغییر وضعیت معتبر نیست.");
        }

        try
        {
            await _workItemService.ChangeStatusAsync(
                id,
                targetStatus);

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

        if (!string.IsNullOrEmpty(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
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
        if (id <= 0 || id != model.Id)
        {
            return BadRequest("شناسهٔ تسک معتبر نیست.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _workItemService.ChangeTitleAsync(
                id,
                model.Title);

            TempData["SuccessMessage"] = "عنوان تسک ویرایش شد.";

            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException)
        {
            return NotFound("تسک پیدا نشد.");
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(
                nameof(model.Title),
                exception.Message);

            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Assign(
        int? id,
        int? projectId)
    {
        if (!ModelState.IsValid ||
            id is <= 0 ||
            projectId is <= 0)
        {
            return BadRequest();
        }

        var model = new AssignWorkItemViewModel
        {
            Id = id,
            ProjectId = projectId
        };

        if (id.HasValue)
        {
            var item = await _workItemService.GetByIdAsync(id.Value);

            if (item is null)
            {
                return NotFound();
            }

            if (projectId.HasValue &&
                projectId.Value != item.ProjectId)
            {
                return BadRequest();
            }

            model.ProjectId = item.ProjectId;
            model.AssignedProjectMemberId = item.AssignedProjectMemberId;
        }

        if (!await LoadAssignOptionsAsync(model))
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(
        AssignWorkItemViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _workItemService.ChangeAssigneeAsync(
                    model.Id!.Value,
                    model.ProjectId!.Value,
                    model.AssignedProjectMemberId!.Value);

                TempData["SuccessMessage"] = "مسئول تسک ذخیره شد.";

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
                    string.Empty,
                    exception.Message);
            }
        }

        if (!await LoadAssignOptionsAsync(model))
        {
            ModelState.AddModelError(
                nameof(model.ProjectId),
                "پروژهٔ انتخاب‌شده وجود ندارد.");
        }

        return View(model);
    }

    [HttpGet]
    [ResponseCache(
        NoStore = true,
        Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ProjectOptions(
        int projectId,
        bool includeTasks = false)
    {
        if (!ModelState.IsValid || projectId <= 0)
        {
            return BadRequest();
        }

        var project = await _projectService.GetByIdAsync(projectId);

        if (project is null)
        {
            return NotFound();
        }

        var members = await _projectMemberService
            .GetByProjectIdAsync(projectId);

        var tasks = includeTasks
            ? await _workItemService.GetByProjectIdAsync(projectId)
            : new List<WorkItemListItemDto>();

        return Json(new
        {
            members = members.Select(member => new
            {
                id = member.Id,
                name = member.FullName
            }),

            tasks = tasks.Select(task => new
            {
                id = task.Id,
                title = task.Title,
                assigneeId = task.AssignedProjectMemberId
            })
        });
    }

    private async Task<bool> LoadCreateOptionsAsync(
        CreateWorkItemViewModel model)
    {
        model.Projects = await ProjectOptionsAsync();
        model.Members = new();

        if (model.ProjectId is not > 0)
        {
            return true;
        }

        bool projectExists = model.Projects.Any(
            project => project.Value == model.ProjectId.Value.ToString());

        if (!projectExists)
        {
            return false;
        }

        model.Members = await MemberOptionsAsync(
            model.ProjectId.Value);

        return true;
    }

    private async Task<bool> LoadAssignOptionsAsync(
        AssignWorkItemViewModel model)
    {
        model.Projects = await ProjectOptionsAsync();
        model.WorkItems = new();
        model.Members = new();

        if (model.ProjectId is not > 0)
        {
            return true;
        }

        bool projectExists = model.Projects.Any(
            project => project.Value == model.ProjectId.Value.ToString());

        if (!projectExists)
        {
            return false;
        }

        model.WorkItems = await _workItemService
            .GetByProjectIdAsync(model.ProjectId.Value);

        model.Members = await MemberOptionsAsync(
            model.ProjectId.Value);

        return true;
    }

    private async Task<List<SelectListItem>> ProjectOptionsAsync()
    {
        var projects = await _projectService.GetAllAsync();

        return projects
            .OrderBy(project => project.Name)
            .Select(project => new SelectListItem
            {
                Value = project.Id.ToString(),
                Text = project.Name
            })
            .ToList();
    }

    private async Task<List<SelectListItem>> MemberOptionsAsync(
        int projectId)
    {
        var members = await _projectMemberService
            .GetByProjectIdAsync(projectId);

        return members
            .Select(member => new SelectListItem
            {
                Value = member.Id.ToString(),
                Text = member.FullName
            })
            .ToList();
    }
}