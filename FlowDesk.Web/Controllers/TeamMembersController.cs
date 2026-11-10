using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.TeamMembers;
using FlowDesk.Web.ViewModels.TeamMembers;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.Web.Controllers;

public class TeamMembersController : Controller
{
    private readonly ITeamMemberService _teamMemberService;

    public TeamMembersController(
        ITeamMemberService teamMemberService)
    {
        _teamMemberService = teamMemberService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var members = await _teamMemberService.GetAllAsync();

        var model = members.Select(member =>
            new TeamMemberListItemViewModel
            {
                Id = member.Id,
                FullName = member.FullName
            }).ToList();

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateTeamMemberViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateTeamMemberViewModel model)
    {
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
                nameof(model.FullName), exception.Message);

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

            TempData["SuccessMessage"] = "عضو حذف شد.";
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