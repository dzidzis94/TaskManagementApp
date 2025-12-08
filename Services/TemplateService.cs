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
    }
}
