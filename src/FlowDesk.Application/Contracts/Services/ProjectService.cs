using FlowDesk.Application.Common;
using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.DTOs.Projects;
using FlowDesk.Domain.Common;
using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Contracts.Services;

public class ProjectService : IProjectService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ProjectAccess _access;

    public ProjectService(
        IRepository<Project> projectRepository,
        IUnitOfWork unitOfWork, ProjectAccess access)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _access = access;
    }

    public async Task<int> CreateAsync(CreateProjectDto input)
    {
        var scope = await _access.GetScopeAsync();
        var project = Project.CreateManaged(input.Name, scope.UserId);

        int duplicateCount = await _projectRepository.CountAsync(
            existing => existing.Name == project.Name);

        if (duplicateCount > 0)
        {
            throw new InvalidOperationException(
                "پروژه‌ای با این نام قبلاً ثبت شده است.");
        }

        await _projectRepository.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        return project.Id;
    }

    public async Task<List<ProjectListItemDto>> GetAllAsync()
    {
        var scope = await _access.GetScopeAsync();
        var ids = scope.VisibleProjectIds;
        var projects = await _projectRepository.FindAsync(x => ids.Contains(x.Id));

        return projects.Select(ToListItem).ToList();
    }

    public async Task<ProjectListItemDto?> GetByIdAsync(int id)
    {
        (await _access.GetScopeAsync()).RequireView(id);
        var project = await _projectRepository.GetByIdAsync(id);
        return project is null ? null : ToListItem(project);
    }

    public async Task RenameAsync(int id, string name)
    {
        (await _access.GetScopeAsync()).RequireManager(id);
        var project = await _projectRepository.GetByIdAsync(id);

        if (project is null)
        {
            throw new KeyNotFoundException(
                "پروژه پیدا نشد.");
        }

        string cleanName = TextRules.RequiredName(
            name,
            nameof(name),
            "نام پروژه");

        int duplicateCount = await _projectRepository.CountAsync(
            existing =>
                existing.Id != id &&
                existing.Name == cleanName);

        if (duplicateCount > 0)
        {
            throw new InvalidOperationException(
                "پروژهٔ دیگری با این نام وجود دارد.");
        }

        project.Rename(cleanName);

        await _unitOfWork.SaveChangesAsync();
    }
    private static ProjectListItemDto ToListItem(Project project)
    {
        return new ProjectListItemDto
        {
            Id = project.Id,
            Name = project.Name,
            ManagerUserId = project.ManagerUserId
        };
    }
}
