using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManagementApp.Data;
using TaskManagementApp.Models;
using TaskManagementApp.Services.Interfaces;

namespace TaskManagementApp.Services
{
    /// <summary>
    /// Service for managing project-related operations.
    /// </summary>
    public class ProjectService : IProjectService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectService> _logger;

        public ProjectService(ApplicationDbContext context, ILogger<ProjectService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects.ToListAsync();
        }

        public async Task<Project> GetProjectByIdAsync(int id)
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task CreateProjectAsync(Project project, int? templateId)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            _context.Add(project);
            await _context.SaveChangesAsync();

            if (templateId.HasValue)
            {
                var template = await _context.ProjectTemplates
                    .Include(t => t.Sections)
                    .ThenInclude(s => s.ChildSections) // Eagerly load the entire hierarchy
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == templateId.Value);

                if (template != null)
                {
                    var rootSections = template.Sections.Where(s => s.ParentSectionId == null);
                    foreach (var section in rootSections)
                    {
                        CreateTaskFromSection(section, project, null);
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        private void CreateTaskFromSection(TemplateSection section, Project project, TaskItem parentTask)
        {
            var task = new TaskItem
            {
                Title = section.Title,
                Description = section.Description,
                ProjectId = project.Id,
                ParentTask = parentTask,
                Priority = section.Priority,
                DueDate = section.DueDateOffsetDays.HasValue
                    ? DateTime.UtcNow.AddDays(section.DueDateOffsetDays.Value)
                    : (DateTime?)null
            };
            _context.Tasks.Add(task);

            if (section.ChildSections != null)
            {
                foreach (var childSection in section.ChildSections)
                {
                    CreateTaskFromSection(childSection, project, task);
                }
            }
        }

        public async Task UpdateProjectAsync(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            _context.Update(project);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProjectAsync(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                _logger.LogWarning("DeleteProjectAsync: Project with ID {ProjectId} not found.", id);
                return; // Or throw a specific exception
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var allTasksInProject = await _context.Tasks
                        .Where(t => t.ProjectId == id)
                        .ToListAsync();

                    if (allTasksInProject.Any())
                    {
                        await DeleteTaskHierarchy(allTasksInProject);
                    }

                    _context.Projects.Remove(project);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.LogInformation("Project {ProjectId} and all associated data deleted successfully.", id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error deleting project {ProjectId}. Transaction rolled back.", id);
                    throw; // Re-throw to be handled by the controller
                }
            }
        }

        public async Task DeleteMultipleProjectsAsync(int[] projectIds)
        {
            if (projectIds == null || !projectIds.Any())
            {
                return;
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var projectsToDelete = await _context.Projects
                        .Where(p => projectIds.Contains(p.Id))
                        .ToListAsync();

                    var allTasksToDelete = await _context.Tasks
                        .Where(t => t.ProjectId.HasValue && projectIds.Contains(t.ProjectId.Value))
                        .ToListAsync();

                    if (allTasksToDelete.Any())
                    {
                        await DeleteTaskHierarchy(allTasksToDelete);
                    }

                    _context.Projects.RemoveRange(projectsToDelete);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during bulk deletion of projects. Transaction rolled back.");
                    throw;
                }
            }
        }

        private async Task DeleteTaskHierarchy(List<TaskItem> tasks)
        {
            var taskIds = tasks.Select(t => t.Id).ToList();

            var assignments = await _context.TaskAssignments.Where(a => taskIds.Contains(a.TaskId)).ToListAsync();
            var completions = await _context.TaskCompletions.Where(c => taskIds.Contains(c.TaskId)).ToListAsync();

            if (assignments.Any()) _context.TaskAssignments.RemoveRange(assignments);
            if (completions.Any()) _context.TaskCompletions.RemoveRange(completions);

            _context.Tasks.RemoveRange(tasks);
        }

        public async Task<bool> ProjectExistsAsync(int id)
        {
            return await _context.Projects.AnyAsync(e => e.Id == id);
        }

        public async Task<object> GetTemplatePreviewAsync(int id)
        {
            var template = await _context.ProjectTemplates
                .Include(t => t.Sections)
                .ThenInclude(s => s.ChildSections)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
            {
                return null;
            }

            var rootSections = template.Sections.Where(s => s.ParentSectionId == null);

            Func<TemplateSection, object> sectionSelector = null;
            sectionSelector = s => new
            {
                s.Title,
                s.Description,
                Priority = s.Priority.ToString(),
                s.DueDateOffsetDays,
                Children = s.ChildSections.Select(child => sectionSelector(child))
            };

            return rootSections.Select(s => sectionSelector(s));
        }

        public async Task<IEnumerable<ProjectTemplate>> GetAllProjectTemplatesAsync()
        {
            return await _context.ProjectTemplates.ToListAsync();
        }
    }
}