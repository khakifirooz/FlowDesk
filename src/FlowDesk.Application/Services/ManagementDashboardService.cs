using FlowDesk.Application.Common;
using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.DTOs.Dashboard;

namespace FlowDesk.Application.Services;

public sealed class ManagementDashboardService(
    IManagementDashboardReader reader, ICurrentUser currentUser, TimeProvider clock)
{
    public Task<ManagementDashboardDto?> GetAsync(int? projectId, CancellationToken cancellationToken)
    {
        string userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("ابتدا وارد حساب شوید.");

        return reader.ReadAsync(userId, projectId, FlowDeskCalendar.Today(clock), cancellationToken);
    }
}
