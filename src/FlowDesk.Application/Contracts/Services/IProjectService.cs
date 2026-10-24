using FlowDesk.Application.DTOs.Projects;

namespace FlowDesk.Application.Contracts.Services;

public interface IProjectService
{
    Task<int> CreateAsync(CreateProjectDto input);
}
