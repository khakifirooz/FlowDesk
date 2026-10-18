using FlowDesk.Domain.Entities;
using FlowDesk.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FlowDesk.Web.Controllers;

public class PracticeController : Controller
{
    public IActionResult Index()
    {
        var loginTask = new WorkItem("Design login page", 1);
        loginTask.Start();

        // TODO 1: Finish loginTask using its method.
        loginTask.Complete();

        var cartTask = new WorkItem("Design shopping cart", 2);
        cartTask.Start();
        cartTask.Complete();

        var registrationTask =
            new WorkItem("Create registration", 1);

        var workItems = new[]
        {
            loginTask,
            cartTask,
            registrationTask
        };

        int targetProjectId = 1;
        int doneCount = 0;

        foreach (var workItem in workItems)
        {
            // TODO 2: Compare this item's project with targetProjectId.

            bool belongsToProject = false;

            if (workItem.ProjectId == targetProjectId)
            {
                belongsToProject = true;
            }

            // TODO 3: Check whether this item is Done.
            bool isDone = workItem.Status == WorkItemStatus.Done;

            if (belongsToProject && isDone)
            {
                doneCount++;
            }
        }

        return Json(new
        {
            targetProjectId,
            doneCount,
            loginStatus = loginTask.Status.ToString(),
            cartStatus = cartTask.Status.ToString(),
            registrationStatus = registrationTask.Status.ToString()
        });
    }
}