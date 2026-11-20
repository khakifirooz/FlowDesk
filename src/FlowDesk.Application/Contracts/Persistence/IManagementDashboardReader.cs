using FlowDesk.Application.DTOs.Dashboard;

namespace FlowDesk.Application.Contracts.Persistence;

public interface IManagementDashboardReader
{
    // Returns null for a requested project outside the manager's scope.
    Task<ManagementDashboardDto?> ReadAsync(
        string managerUserId, int? projectId, DateOnly today, CancellationToken cancellationToken);
}
