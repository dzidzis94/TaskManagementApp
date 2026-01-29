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
        private readonly ITaskService _taskService;

        public TemplateService(ApplicationDbContext context, ITaskService taskService)
        {
            _context = context;
            _taskService = taskService;
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

        public async Task<Project> CloneProjectAsync(int sourceProjectId, string newProjectName, string newProjectDescription, string userId, List<int>? excludedTaskIds = null)
        {
            var sourceProject = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == sourceProjectId);

            if (sourceProject == null)
            {
                throw new ArgumentException("Source project not found.");
            }

            // Create and save the new project first to get a valid ID
            var targetProject = new Project
            {
                Name = newProjectName,
                Description = newProjectDescription,
                IsPublic = sourceProject.IsPublic, // Inherit public status
                CreatedAt = DateTime.UtcNow
            };
            _context.Projects.Add(targetProject);
            await _context.SaveChangesAsync(); // This generates the targetProject.Id

            // Efficiently fetch all tasks for the source project in a single query
            var allTasks = await _context.Tasks
                .Where(t => t.ProjectId == sourceProjectId)
                .AsNoTracking()
                .ToListAsync();

            // Build the hierarchy in memory
            var rootTasks = HierarchyHelper.BuildTaskHierarchy(allTasks);

            foreach (var task in rootTasks)
            {
                if (excludedTaskIds != null && excludedTaskIds.Contains(task.Id))
                {
                    continue;
                }

                await _taskService.CloneTaskTreeAsync(task.Id, targetProject.Id, userId, null, excludedTaskIds);
            }

            return targetProject;
        }
    }
}
