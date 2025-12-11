using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManagementApp.Data;
using TaskManagementApp.Models;
using TaskManagementApp.Services.Interfaces;
using TaskManagementApp.Helpers;

namespace TaskManagementApp.Services
{
    public class TemplateService : ITemplateService
    {
        private readonly ApplicationDbContext _context;

        public TemplateService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Project> GenerateProjectFromTemplateAsync(int templateId, Project targetProject)
        {
            var projectTemplate = await _context.ProjectTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(pt => pt.Id == templateId);

            if (projectTemplate == null)
            {
                throw new ArgumentException("Template not found.");
            }

            // Efficiently fetch all sections for the template in a single query
            var allSections = await _context.TemplateSections
                .Where(s => s.ProjectTemplateId == templateId)
                .AsNoTracking()
                .ToListAsync();

            // Build the hierarchy in memory
            var sectionDict = allSections.ToDictionary(s => s.Id);
            var rootSections = new List<TemplateSection>();
            foreach (var section in allSections)
            {
                if (section.ParentSectionId.HasValue && sectionDict.TryGetValue(section.ParentSectionId.Value, out var parent))
                {
                    if (parent.ChildSections == null)
                    {
                        parent.ChildSections = new List<TemplateSection>();
                    }
                    parent.ChildSections.Add(section);
                }
                else
                {
                    rootSections.Add(section);
                }
            }

            var tasksToAdd = new List<TaskItem>();
            foreach (var section in rootSections)
            {
                // The recursive method builds the object graph in memory
                tasksToAdd.Add(CreateTaskFromSectionRecursive(section, targetProject, null));
            }

            // Add the entire graph of new tasks to the context.
            // EF Core's change tracker will automatically discover all child entities.
            _context.Tasks.AddRange(tasksToAdd);

            // Save all changes in a single transaction
            await _context.SaveChangesAsync();

            return targetProject;
        }

        private TaskItem CreateTaskFromSectionRecursive(TemplateSection section, Project project, TaskItem parentTask)
        {
            var task = new TaskItem
            {
                Title = section.Title,
                Description = section.Description,
                Priority = section.Priority,
                Project = project,
                ParentTask = parentTask,
                DueDate = section.DueDateOffsetDays.HasValue ? DateTime.UtcNow.AddDays(section.DueDateOffsetDays.Value) : (DateTime?)null,
                SubTasks = new List<TaskItem>() // Initialize the collection
            };

            if (section.ChildSections != null)
            {
                foreach (var childSection in section.ChildSections)
                {
                    var childTask = CreateTaskFromSectionRecursive(childSection, project, task);
                    task.SubTasks.Add(childTask);
                }
            }

            return task;
        }

        public async Task<Project> CloneProjectAsync(int sourceProjectId, string newProjectName, string? newProjectDescription, List<int>? excludedTaskIds = null)
        {
            var sourceProject = await _context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == sourceProjectId);
            if (sourceProject == null) throw new ArgumentException("Source project not found.");

            var targetProject = new Project
            {
                Name = newProjectName,
                Description = newProjectDescription,
                IsPublic = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(targetProject);
            await _context.SaveChangesAsync();

            // Efficiently fetch all tasks for the source project in a single query
            var allTasks = await _context.Tasks
                .Where(t => t.ProjectId == sourceProjectId)
                .AsNoTracking()
                .ToListAsync();

            // Build the hierarchy in memory
            var rootTasks = HierarchyHelper.BuildTaskHierarchy(allTasks);

            var excludedTaskIdsSet = excludedTaskIds != null ? new HashSet<int>(excludedTaskIds) : new HashSet<int>();
            var tasksToAdd = new List<TaskItem>();
            foreach (var task in rootTasks)
            {
                var newTask = CreateTaskFromTaskRecursive(task, targetProject, null, excludedTaskIdsSet);
                if (newTask != null)
                {
                    tasksToAdd.Add(newTask);
                }
            }

            _context.Tasks.AddRange(tasksToAdd);
            await _context.SaveChangesAsync();

            return targetProject;
        }

        private TaskItem? CreateTaskFromTaskRecursive(TaskItem sourceTask, Project project, TaskItem? parentTask, HashSet<int> excludedTaskIds)
        {
            if (excludedTaskIds.Contains(sourceTask.Id))
            {
                return null; // Skip this task and its entire subtree
            }

            var task = new TaskItem
            {
                Title = sourceTask.Title,
                Description = sourceTask.Description,
                Priority = sourceTask.Priority,
                Status = TaskManagementApp.Models.TaskStatus.Pending, // Reset status to Pending
                DueDate = sourceTask.DueDate,
                Project = project,
                ParentTask = parentTask,
                SubTasks = new List<TaskItem>(),
                TaskAssignments = new List<TaskAssignment>() // Do not copy assignees
            };

            if (sourceTask.SubTasks != null)
            {
                foreach (var childSourceTask in sourceTask.SubTasks)
                {
                    var childTask = CreateTaskFromTaskRecursive(childSourceTask, project, task, excludedTaskIds);
                    if (childTask != null)
                    {
                        task.SubTasks.Add(childTask);
                    }
                }
            }

            return task;
        }
    }
}
