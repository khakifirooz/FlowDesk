using FlowDesk.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.Web.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class DashboardController(ManagementDashboardService dashboard) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? projectId, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || projectId is <= 0)
            return BadRequest("شناسهٔ پروژه معتبر نیست.");

        var report = await dashboard.GetAsync(projectId, cancellationToken);
        return report is null ? NotFound() : View(report);
    }
}
