using System.Security.Claims;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.WorkItems;
using FlowDesk.Domain.Enums;
using FlowDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using FlowDesk.Web.ViewModels.WorkItems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FlowDesk.Web.Controllers;

[Authorize]
public class WorkItemsController : Controller
{
    private readonly IWorkItemService _items;
    private readonly IProjectService _projects;
    private readonly IProjectMemberService _members;
    private readonly UserManager<ApplicationUser> _accounts;
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public WorkItemsController(IWorkItemService items, IProjectService projects,
        IProjectMemberService members, UserManager<ApplicationUser> accounts)
    {
        _items = items;
        _projects = projects;
        _members = members;
        _accounts = accounts;
    }

    [HttpGet]
    public async Task<IActionResult> Index(WorkItemIndexViewModel model)
    {
        var projects = await _projects.GetAllAsync();
        model.CanCreate = projects.Any(x => x.ManagerUserId == UserId);
        model.Projects = projects.OrderBy(x => x.Name).Select(x =>
            new SelectListItem(x.Name, x.Id.ToString())).ToList();
        if (!ModelState.IsValid) return View(model);

        var result = await _items.SearchAsync(new WorkItemSearchDto
        {
            OnlyMine = model.OnlyMine, SearchText = model.SearchText, Archived = model.Archived,
            Priority = model.Priority, ProjectId = model.ProjectId, Status = model.Status, PageNumber = model.PageNumber
        });
        model.TotalCount = result.TotalCount;
        model.HasLinkedMembership = result.HasLinkedMembership;
        model.PersonalCountWithoutFilters = result.PersonalCountWithoutFilters;
        model.PageSize = result.PageSize;
        model.PageNumber = result.PageNumber;
        var projectNames = projects.ToDictionary(x => x.Id, x => x.Name);
        var memberIds = result.Items.Where(x => x.AssignedProjectMemberId.HasValue)
            .Select(x => x.AssignedProjectMemberId!.Value).Distinct().ToList();
        var names = await _members.GetNamesByIdsAsync(memberIds);
        model.Items = result.Items.Select(item => new WorkItemListItemViewModel
        {
            Id = item.Id, Title = item.Title, Status = item.Status,
            Priority = item.Priority, DueDate = item.DueDate, ArchivedAtUtc = item.ArchivedAtUtc,
            CanManage = item.CanManage, CanChangeStatus = item.CanChangeStatus,
            AssignedProjectMemberId = item.AssignedProjectMemberId,
            AssignedMemberName = item.AssignedProjectMemberId is int id && names.TryGetValue(id, out var name)
                ? name : "بدون مسئول",
            ProjectName = projectNames.TryGetValue(item.ProjectId, out var projectName)
                ? projectName : "—",
            StatusText = WorkItemLabels.Status(item.Status)
        }).ToList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? projectId)
    {
        if (!ModelState.IsValid || projectId is <= 0) return BadRequest();
        var model = new CreateWorkItemViewModel { ProjectId = projectId };
        if (!await LoadCreateAsync(model)) return NotFound();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWorkItemViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                int id = await _items.CreateAsync(new CreateWorkItemDto
                {
                    Title = model.Title, ProjectId = model.ProjectId!.Value,
                    Description = model.Description, Priority = model.Priority, DueDate = model.DueDate,
                    AssignedProjectMemberId = model.AssignedProjectMemberId!.Value
                });
                TempData["SuccessMessage"] = $"تسک با شناسهٔ {id} ثبت شد.";
                return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }
        if (!await LoadCreateAsync(model)) return NotFound();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _items.GetByIdAsync(id);
        if (item is null) return NotFound();
        if (!item.CanManage) return Forbid();
        if (item.ArchivedAtUtc is not null) return RedirectToAction(nameof(Details), new { id });
        return View(new EditWorkItemViewModel
        {
            Id = item.Id, Title = item.Title, Description = item.Description,
            Priority = item.Priority, DueDate = item.DueDate
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditWorkItemViewModel model)
    {
        if (id <= 0 || id != model.Id) return BadRequest();
        var item = await _items.GetByIdAsync(id);
        if (item is null) return NotFound();
        if (!item.CanManage) return Forbid();
        if (!ModelState.IsValid) return View(model);
        try
        {
            await _items.EditAsync(id, new EditWorkItemDto(model.Title, model.Description, model.Priority, model.DueDate));
            TempData["SuccessMessage"] = "تسک ویرایش شد.";
            return RedirectToAction(nameof(Index), new { projectId = item.ProjectId });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, WorkItemStatus targetStatus, string? returnUrl)
    {
        if (!ModelState.IsValid || id <= 0 || !Enum.IsDefined(targetStatus)) return BadRequest();
        try
        {
            await _items.ChangeStatusAsync(id, targetStatus);
            TempData["SuccessMessage"] = "وضعیت تسک تغییر کرد.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Assign(int? id, int? projectId)
    {
        if (!ModelState.IsValid || id is <= 0 || projectId is <= 0) return BadRequest();
        var model = new AssignWorkItemViewModel { Id = id, ProjectId = projectId };
        if (id.HasValue)
        {
            var item = await _items.GetByIdAsync(id.Value);
            if (item is null) return NotFound();
            if (!item.CanManage) return Forbid();
            if (item.ArchivedAtUtc is not null) return RedirectToAction(nameof(Details), new { id });
            if (projectId.HasValue && item.ProjectId != projectId) return BadRequest();
            model.ProjectId = item.ProjectId;
            model.AssignedProjectMemberId = item.AssignedProjectMemberId;
        }
        if (!await LoadAssignAsync(model)) return NotFound();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignWorkItemViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await _items.ChangeAssigneeAsync(model.Id!.Value, model.ProjectId!.Value,
                    model.AssignedProjectMemberId!.Value);
                TempData["SuccessMessage"] = "مسئول تسک ذخیره شد.";
                return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { ModelState.AddModelError(string.Empty, ex.Message); }
        }
        if (!await LoadAssignAsync(model)) return NotFound();
        return View(model);
    }

    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ProjectOptions(int projectId, bool includeTasks = false)
    {
        if (!ModelState.IsValid || projectId <= 0) return BadRequest();
        var project = await _projects.GetByIdAsync(projectId);
        if (project is null) return NotFound();
        if (project.ManagerUserId != UserId) return Forbid();
        var members = await _members.GetByProjectIdAsync(projectId);
        var tasks = includeTasks ? await _items.GetByProjectIdAsync(projectId) : new List<WorkItemListItemDto>();
        return Json(new
        {
            members = members.Select(x => new { id = x.Id, name = x.FullName }),
            tasks = tasks.Select(x => new { id = x.Id, title = x.Title, assigneeId = x.AssignedProjectMemberId })
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, int activityPage = 1)
    {
        if (!ModelState.IsValid || id <= 0) return BadRequest();
        var details = await _items.GetDetailsAsync(id, activityPage);
        if (details is null) return NotFound();
        var project = await _projects.GetByIdAsync(details.Item.ProjectId);
        var assignee = "بدون مسئول";
        if (details.Item.AssignedProjectMemberId is int membershipId)
        {
            var names = await _members.GetNamesByIdsAsync(new List<int> { membershipId });
            if (names.TryGetValue(membershipId, out var name)) assignee = name;
        }
        return View(new WorkItemDetailsViewModel(details, project?.Name ?? "—", assignee));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment([FromForm] int id, [FromForm] string? comment)
    {
        if (!ModelState.IsValid || id <= 0) return BadRequest();
        var account = await _accounts.GetUserAsync(User);
        if (account is null) return Forbid();
        try
        {
            await _items.AddCommentAsync(id, comment,
                string.IsNullOrWhiteSpace(account.FullName) ? "عضو پروژه" : account.FullName);
            TempData["SuccessMessage"] = "نظر ثبت شد.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetArchived([FromForm] int id, [FromForm] bool? archived)
    {
        if (!ModelState.IsValid || id <= 0 || !archived.HasValue) return BadRequest();
        try
        {
            await _items.SetArchivedAsync(id, archived.Value);
            TempData["SuccessMessage"] = archived.Value ? "تسک بایگانی شد." : "تسک به فهرست فعال برگشت.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<List<SelectListItem>> ManagedProjectsAsync()
    {
        var projects = await _projects.GetAllAsync();
        return projects.Where(x => x.ManagerUserId == UserId).OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
    }

    private async Task<List<SelectListItem>> MemberOptionsAsync(int projectId)
    {
        var members = await _members.GetByProjectIdAsync(projectId);
        return members.Select(x => new SelectListItem(x.FullName, x.Id.ToString())).ToList();
    }

    private async Task<bool> LoadCreateAsync(CreateWorkItemViewModel model)
    {
        model.Projects = await ManagedProjectsAsync();
        model.Members = new();
        if (model.ProjectId is not > 0) return true;
        if (!model.Projects.Any(x => x.Value == model.ProjectId.Value.ToString())) return false;
        model.Members = await MemberOptionsAsync(model.ProjectId.Value);
        return true;
    }

    private async Task<bool> LoadAssignAsync(AssignWorkItemViewModel model)
    {
        model.Projects = await ManagedProjectsAsync();
        model.WorkItems = new(); model.Members = new();
        if (model.ProjectId is not > 0) return true;
        if (!model.Projects.Any(x => x.Value == model.ProjectId.Value.ToString())) return false;
        model.WorkItems = await _items.GetByProjectIdAsync(model.ProjectId.Value);
        model.Members = await MemberOptionsAsync(model.ProjectId.Value);
        return true;
    }
}
