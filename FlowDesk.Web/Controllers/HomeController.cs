using System.Diagnostics;
using FlowDesk.Application.Common;
using FlowDesk.Web.Models;
using FlowDesk.Web.ViewModels.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.Web.Controllers;

public class HomeController(ProjectAccess access) : Controller
{
    [AllowAnonymous]
    [AcceptVerbs("GET", "HEAD")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return View();

        var scope = await access.GetViewScopeAsync();
        return View(new WorkspaceHomeViewModel(
            scope.ManagedProjectIds.Count > 0, scope.OwnMembershipIds.Count > 0));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId =
                Activity.Current?.Id ??
                HttpContext.TraceIdentifier
        });
    }
}