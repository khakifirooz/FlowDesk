using FlowDesk.Application.Contracts.Persistence;
using FlowDesk.Application.DTOs.Projects;
using FlowDesk.Domain.Common;
using FlowDesk.Domain.Entities;

namespace FlowDesk.Application.Contracts.Services;

public class ProjectService : IProjectService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProjectService(
        IRepository<Project> projectRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CreateAsync(CreateProjectDto input)
    {
        var project = new Project(input.Name);

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
        var projects = await _projectRepository.GetAllAsync();

        var result = new List<ProjectListItemDto>();

        foreach (var project in projects)
        {
            result.Add(new ProjectListItemDto
            {
                Id = project.Id,
                Name = project.Name
            });
        }

        return result;
    }

    public async Task<ProjectListItemDto?> GetByIdAsync(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id);

        if (project is null)
        {
            return null;
        }

        return new ProjectListItemDto
        {
            Id = project.Id,
            Name = project.Name
        };
    }

    public async Task RenameAsync(int id, string name)
    {
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
}