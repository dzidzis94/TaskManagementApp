using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementApp.Data;
using TaskManagementApp.Models;
using System.Threading.Tasks;

namespace TaskManagementApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProjectsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var projects = await _context.Projects.ToListAsync();
            return View(projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Include(p => p.Tasks)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.ProjectTemplates = await _context.ProjectTemplates.ToListAsync();
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,IsPublic")] Project project, int? templateId)
        {
            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync(); // First save to get the project Id

                if (templateId.HasValue)
                {
                    var template = await _context.ProjectTemplates
                        .Include(t => t.Sections) // Include all sections related to the template
                        .FirstOrDefaultAsync(t => t.Id == templateId.Value);

                    if (template != null)
                    {
                        // Manually build the hierarchy
                        var sectionMap = template.Sections.ToDictionary(s => s.Id);
                        foreach (var section in template.Sections)
                        {
                            if (section.ParentSectionId.HasValue && sectionMap.ContainsKey(section.ParentSectionId.Value))
                            {
                                var parent = sectionMap[section.ParentSectionId.Value];
                                if (parent.ChildSections == null)
                                {
                                    parent.ChildSections = new List<TemplateSection>();
                                }
                                parent.ChildSections.Add(section);
                            }
                        }

                        // Process only the root sections
                        foreach (var section in template.Sections.Where(s => s.ParentSectionId == null))
                        {
                            CreateTaskFromSection(section, project, null);
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Project created successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.ProjectTemplates = await _context.ProjectTemplates.ToListAsync();
            return View(project);
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

            // Note: We need to load ChildSections explicitly if they aren't already.
            // This implementation assumes they are loaded.
            if (section.ChildSections != null)
            {
                foreach (var childSection in section.ChildSections)
                {
                    CreateTaskFromSection(childSection, project, task);
                }
            }
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            return View(project);
        }

        // POST: Projects/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,IsPublic")] Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Project updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Recursively delete all tasks and their descendants
                    var allTasks = await _context.Tasks
                        .Where(t => t.ProjectId == id)
                        .ToListAsync();

                    RemoveTasks(allTasks);

                    _context.Projects.Remove(project);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Project and all its tasks have been deleted successfully!";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Log the error
                    TempData["ErrorMessage"] = "An error occurred while deleting the project. The operation was rolled back.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private void RemoveTasks(IEnumerable<TaskItem> tasks)
        {
            foreach (var task in tasks)
            {
                // Load and remove assignments and completions if they are not loaded automatically
                var assignments = _context.TaskAssignments.Where(a => a.TaskId == task.Id);
                _context.TaskAssignments.RemoveRange(assignments);

                var completions = _context.TaskCompletions.Where(c => c.TaskId == task.Id);
                _context.TaskCompletions.RemoveRange(completions);
            }
            _context.Tasks.RemoveRange(tasks);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple(int[] projectIds)
        {
            if (projectIds == null || projectIds.Length == 0)
            {
                TempData["ErrorMessage"] = "No projects selected for deletion.";
                return RedirectToAction(nameof(Index));
            }

            var projectsToDelete = await _context.Projects
                .Where(p => projectIds.Contains(p.Id))
                .Include(p => p.Tasks)
                .ToListAsync();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var project in projectsToDelete)
                    {
                        var allTasks = await _context.Tasks
                            .Where(t => t.ProjectId == project.Id)
                            .ToListAsync();

                        RemoveTasks(allTasks);
                        _context.Projects.Remove(project);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"{projectsToDelete.Count} project(s) have been deleted successfully.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Log the error
                    TempData["ErrorMessage"] = "An error occurred while deleting the projects. The operation was rolled back.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplatePreview(int id)
        {
            var template = await _context.ProjectTemplates
                .Include(t => t.Sections)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            var sections = template.Sections.ToList();
            var sectionMap = sections.ToDictionary(s => s.Id);
            var rootSections = new List<TemplateSection>();

            foreach (var section in sections)
            {
                if (section.ParentSectionId.HasValue && sectionMap.ContainsKey(section.ParentSectionId.Value))
                {
                    var parent = sectionMap[section.ParentSectionId.Value];
                    if (parent.ChildSections == null) parent.ChildSections = new List<TemplateSection>();
                    parent.ChildSections.Add(section);
                }
                else
                {
                    rootSections.Add(section);
                }
            }

            // Use anonymous types to avoid circular references
            Func<TemplateSection, object> sectionSelector = null;
            sectionSelector = s => new
            {
                s.Id,
                s.Title,
                s.Description,
                Priority = s.Priority.ToString(),
                s.DueDateOffsetDays,
                Children = s.ChildSections.Select(child => sectionSelector(child))
            };

            var result = rootSections.Select(s => sectionSelector(s));

            return Json(result);
        }
    }
}