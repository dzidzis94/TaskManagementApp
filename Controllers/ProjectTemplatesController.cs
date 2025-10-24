using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementApp.Data;
using TaskManagementApp.Models;
using System.Threading.Tasks;

namespace TaskManagementApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProjectTemplatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProjectTemplatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ProjectTemplates
        public async Task<IActionResult> Index()
        {
            var projectTemplates = await _context.ProjectTemplates.ToListAsync();
            return View(projectTemplates);
        }

        // GET: ProjectTemplates/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var projectTemplate = await _context.ProjectTemplates
                .Include(pt => pt.Sections)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (projectTemplate == null)
            {
                return NotFound();
            }

            return View(projectTemplate);
        }

        // GET: ProjectTemplates/EditTree/5
        public async Task<IActionResult> EditTree(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var projectTemplate = await _context.ProjectTemplates.FindAsync(id);
            if (projectTemplate == null)
            {
                return NotFound();
            }
            return View(projectTemplate);
        }

        // GET: ProjectTemplates/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ProjectTemplates/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] ProjectTemplate projectTemplate)
        {
            if (ModelState.IsValid)
            {
                _context.Add(projectTemplate);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Project template created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(projectTemplate);
        }

        // GET: ProjectTemplates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var projectTemplate = await _context.ProjectTemplates.FindAsync(id);
            if (projectTemplate == null)
            {
                return NotFound();
            }
            return View(projectTemplate);
        }

        // POST: ProjectTemplates/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] ProjectTemplate projectTemplate)
        {
            if (id != projectTemplate.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(projectTemplate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Project template updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectTemplateExists(projectTemplate.Id))
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
            return View(projectTemplate);
        }

        private bool ProjectTemplateExists(int id)
        {
            return _context.ProjectTemplates.Any(e => e.Id == id);
        }

        // POST: ProjectTemplates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var projectTemplate = await _context.ProjectTemplates.FindAsync(id);
            _context.ProjectTemplates.Remove(projectTemplate);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Project template deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("api/templates/{id}")]
        public async Task<IActionResult> GetTemplateJson(int id)
        {
            var sections = await _context.TemplateSections
                .Where(s => s.ProjectTemplateId == id)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.ParentSectionId
                })
                .ToListAsync();

            return Json(sections);
        }
    }
}