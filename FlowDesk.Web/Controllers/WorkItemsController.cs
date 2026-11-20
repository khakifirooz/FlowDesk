using System.Security.Claims;
using FlowDesk.Application.Common;
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
    private readonly TimeProvider _clock;
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public WorkItemsController(IWorkItemService items, IProjectService projects,
        IProjectMemberService members, UserManager<ApplicationUser> accounts, TimeProvider clock)
    {
        _items = items;
        _projects = projects;
        _members = members;
        _accounts = accounts;
        _clock = clock;
    }

    [HttpGet]
    public async Task<IActionResult> Index(WorkItemIndexViewModel model)
    {
        var projects = await _projects.GetAllAsync();
        model.Today = FlowDeskCalendar.Today(_clock);
        model.HasManagedProjects = projects.Any(x => x.ManagerUserId == UserId);
        model.CanCreate = model.ProjectId is int projectId
            ? projects.Any(x => x.Id == projectId && x.ManagerUserId == UserId)
            : model.HasManagedProjects;
        model.Projects = projects.Where(x => !model.ManagedOnly || x.ManagerUserId == UserId)
            .OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
        if (!ModelState.IsValid)
        {
            foreach (var (field, message) in new[]
            {
                (nameof(model.Focus), "فیلتر اقدام برای این فهرست معتبر نیست."),
                (nameof(model.Status), "وضعیت انتخاب‌شده معتبر نیست."),
                (nameof(model.Priority), "اولویت انتخاب‌شده معتبر نیست."),
                (nameof(model.ProjectId), "شناسهٔ پروژه معتبر نیست."),
                (nameof(model.PageNumber), "شمارهٔ صفحه معتبر نیست.")
            })
            {
                if (!ModelState.TryGetValue(field, out var entry) || entry.Errors.Count == 0)
                    continue;

                entry.Errors.Clear();
                ModelState.AddModelError(field, message);
            }

            return View(model);
        }

        var result = await _items.SearchAsync(new WorkItemSearchDto
        {
            OnlyMine = model.OnlyMine, SearchText = model.SearchText, Archived = model.Archived,
            ManagedOnly = model.ManagedOnly, Focus = model.Focus,
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
        if (!await LoadCreateAsync(model, selectSingleProject: true)) return NotFound();
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
                return RedirectToAction(nameof(Details), new { id });
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
    public async Task<IActionResult> Edit(int id, string? returnUrl)
    {
        var item = await _items.GetByIdAsync(id);
        if (item is null) return NotFound();
        if (!item.CanManage) return Forbid();
        if (item.ArchivedAtUtc is not null) return RedirectToAction(nameof(Details), new { id });
        return View(new EditWorkItemViewModel
        {
            Id = item.Id, Title = item.Title, Description = item.Description, ReturnUrl = SafeReturnUrl(returnUrl),
            Priority = item.Priority, DueDate = item.DueDate
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditWorkItemViewModel model)
    {
        if (id <= 0 || id != model.Id) return BadRequest();
        model.ReturnUrl = SafeReturnUrl(model.ReturnUrl);
        var item = await _items.GetByIdAsync(id);
        if (item is null) return NotFound();
        if (!item.CanManage) return Forbid();
        if (!ModelState.IsValid) return View(model);
        try
        {
            await _items.EditAsync(id, new EditWorkItemDto(model.Title, model.Description, model.Priority, model.DueDate));
            TempData["SuccessMessage"] = "تسک ویرایش شد.";
            return model.ReturnUrl is string destination
                ? LocalRedirect(destination) : RedirectToAction(nameof(Details), new { id });
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus([FromForm] int id, [FromForm] WorkItemStatus? targetStatus,
        [FromForm] string? returnUrl)
    {
        if (!ModelState.IsValid || id <= 0 || targetStatus is not { } status || !Enum.IsDefined(status))
            return BadRequest();
        try
        {
            string? authorName = null;
            if (status == WorkItemStatus.Done)
            {
                authorName = await GetAuthorNameAsync();
                if (authorName is null) return Forbid();
            }

            await _items.ChangeStatusAsync(id, status, authorName);
            TempData["SuccessMessage"] = "وضعیت تسک تغییر کرد.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return SafeReturnUrl(returnUrl) is string destination
            ? LocalRedirect(destination) : RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteTask([FromForm] int id, [FromForm] string? completionNote,
        [FromForm] string? returnUrl, [FromForm] bool returnToList)
    {
        if (!ModelState.IsValid || id <= 0) return BadRequest();
        var authorName = await GetAuthorNameAsync();
        if (authorName is null) return Forbid();

        try
        {
            bool completed = await _items.CompleteAsync(id, completionNote, authorName);
            TempData["SuccessMessage"] = completed ? "تکمیل تسک ثبت شد." : "این تسک قبلاً تکمیل شده است.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        var destination = SafeReturnUrl(returnUrl);
        return returnToList && destination is not null
            ? LocalRedirect(destination)
            : RedirectToAction(nameof(Details), new { id, returnUrl = destination });
    }

    [HttpGet]
    public async Task<IActionResult> Assign(int? id, int? projectId, string? returnUrl)
    {
        if (!ModelState.IsValid || id is <= 0 || projectId is <= 0) return BadRequest();
        var model = new AssignWorkItemViewModel { Id = id, ProjectId = projectId, ReturnUrl = SafeReturnUrl(returnUrl) };
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
        if (!await LoadAssignAsync(model, selectSingleProject: true)) return NotFound();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignWorkItemViewModel model)
    {
        model.ReturnUrl = SafeReturnUrl(model.ReturnUrl);
        if (ModelState.IsValid)
        {
            try
            {
                await _items.ChangeAssigneeAsync(model.Id!.Value, model.ProjectId!.Value,
                    model.AssignedProjectMemberId!.Value);
                TempData["SuccessMessage"] = "مسئول تسک ذخیره شد.";
                return model.ReturnUrl is string destination
                    ? LocalRedirect(destination) : RedirectToAction(nameof(Details), new { id = model.Id });
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
    public async Task<IActionResult> Details(int id, int activityPage = 1, string? returnUrl = null)
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
        return View(new WorkItemDetailsViewModel(details, project?.Name ?? "—", assignee,
            SafeReturnUrl(returnUrl), FlowDeskCalendar.Today(_clock)));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment([FromForm] int id, [FromForm] string? comment, [FromForm] string? returnUrl)
    {
        if (!ModelState.IsValid || id <= 0) return BadRequest();
        var authorName = await GetAuthorNameAsync();
        if (authorName is null) return Forbid();
        try
        {
            await _items.AddCommentAsync(id, comment, authorName);
            TempData["SuccessMessage"] = "نظر ثبت شد.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Details), new { id, returnUrl = SafeReturnUrl(returnUrl) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetArchived([FromForm] int id, [FromForm] bool? archived, [FromForm] string? returnUrl)
    {
        if (!ModelState.IsValid || id <= 0 || !archived.HasValue) return BadRequest();
        try
        {
            await _items.SetArchivedAsync(id, archived.Value);
            TempData["SuccessMessage"] = archived.Value ? "تسک بایگانی شد." : "تسک به فهرست فعال برگشت.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id, returnUrl = SafeReturnUrl(returnUrl) });
    }

    private async Task<string?> GetAuthorNameAsync()
    {
        var account = await _accounts.GetUserAsync(User);
        if (account is null) return null;
        return string.IsNullOrWhiteSpace(account.FullName) ? "عضو پروژه" : account.FullName;
    }

    private string? SafeReturnUrl(string? value) =>
        value is { Length: <= 4096 } && Url.IsLocalUrl(value) ? value : null;

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

    private async Task<bool> LoadCreateAsync(CreateWorkItemViewModel model, bool selectSingleProject = false)
    {
        model.Projects = await ManagedProjectsAsync();
        if (selectSingleProject && model.ProjectId is null && model.Projects.Count == 1)
            model.ProjectId = int.Parse(model.Projects[0].Value, System.Globalization.CultureInfo.InvariantCulture);
        model.Members = new();
        if (model.ProjectId is not > 0) return true;
        if (!model.Projects.Any(x => x.Value == model.ProjectId.Value.ToString())) return false;
        model.Members = await MemberOptionsAsync(model.ProjectId.Value);
        return true;
    }

    private async Task<bool> LoadAssignAsync(AssignWorkItemViewModel model, bool selectSingleProject = false)
    {
        model.Projects = await ManagedProjectsAsync();
        if (selectSingleProject && model.ProjectId is null && model.Projects.Count == 1)
            model.ProjectId = int.Parse(model.Projects[0].Value, System.Globalization.CultureInfo.InvariantCulture);
        model.WorkItems = new(); model.Members = new();
        if (model.ProjectId is not > 0) return true;
        if (!model.Projects.Any(x => x.Value == model.ProjectId.Value.ToString())) return false;
        model.WorkItems = await _items.GetByProjectIdAsync(model.ProjectId.Value);
        model.Members = await MemberOptionsAsync(model.ProjectId.Value);
        return true;
    }
}
