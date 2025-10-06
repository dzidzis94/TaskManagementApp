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
                        .Include(t => t.Sections)
                        .ThenInclude(s => s.ChildSections)
                        .FirstOrDefaultAsync(t => t.Id == templateId.Value);

                    if (template != null)
                    {
                        foreach (var section in template.Sections.Where(s => s.ParentSectionId == null))
                        {
                            CreateTaskFromSection(section, project, null);
                        }
                        await _context.SaveChangesAsync(); // Second save for all the tasks
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
                ParentTask = parentTask
            };
            _context.Tasks.Add(task);

            foreach (var childSection in section.ChildSections)
            {
                CreateTaskFromSection(childSection, project, task);
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
            var project = await _context.Projects.FindAsync(id);
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Project deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
}