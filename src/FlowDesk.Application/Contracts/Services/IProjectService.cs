using FlowDesk.Application.DTOs.Projects;

namespace FlowDesk.Application.Contracts.Services;

public interface IProjectService
{
    Task<int> CreateAsync(CreateProjectDto input);
    Task<List<ProjectListItemDto>> GetAllAsync();

    Task<ProjectListItemDto?> GetByIdAsync(int id);
    Task RenameAsync(int id, string name);
}
