using System.Security.Claims;
using FlowDesk.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FlowDesk.Web.Security;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;
    public HttpCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public string? UserId => _accessor.HttpContext?.User.Identity?.IsAuthenticated == true
        ? _accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
        : null;
}

public sealed class AccessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not UnauthorizedAccessException) return;
        context.Result = context.HttpContext.User.Identity?.IsAuthenticated == true
            ? new ForbidResult()
            : new ChallengeResult();
        context.ExceptionHandled = true;
    }
}
