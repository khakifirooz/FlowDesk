using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.Contracts.Services;
using FlowDesk.Application.DTOs.Projects;
using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Services;

public class ProjectService : IProjectService
{

    private readonly IRepository<Project> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectService(IRepository<Project> projectRepository, IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }
    public async Task<int> CreateAsync(CreateProjectDto input)
    {
        var project = new Project(input.Name);

        await _projectRepository.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();
        return project.Id;
    }

    public async Task<List<ProjectListItemDto>> GetAllAsync()
    {
        var projects = await _projectRepository.GetAllAsync();
        var result = new List<ProjectListItemDto>();
        foreach (var project in projects)
        {
            result.Add(new ProjectListItemDto
            {
                Id = project.Id,
                Name = project.Name,
            });
        }
        return result;
    }
}
