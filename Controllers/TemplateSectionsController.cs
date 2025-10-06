using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementApp.Data;
using TaskManagementApp.Models;
using System.Threading.Tasks;

namespace TaskManagementApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TemplateSectionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TemplateSectionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TemplateSections/Create
        public async Task<IActionResult> Create(int projectTemplateId)
        {
            ViewBag.ProjectTemplateId = projectTemplateId;
            ViewBag.ParentSections = await _context.TemplateSections
                .Where(s => s.ProjectTemplateId == projectTemplateId)
                .ToListAsync();
            return View();
        }

        // POST: TemplateSections/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Order,ProjectTemplateId,ParentSectionId")] TemplateSection templateSection)
        {
            if (ModelState.IsValid)
            {
                _context.Add(templateSection);
                await _context.SaveChangesAsync();
                return RedirectToAction("Edit", "ProjectTemplates", new { id = templateSection.ProjectTemplateId });
            }
            return View(templateSection);
        }

        // GET: TemplateSections/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var templateSection = await _context.TemplateSections.FindAsync(id);
            if (templateSection == null)
            {
                return NotFound();
            }

            ViewBag.ParentSections = await _context.TemplateSections
                .Where(s => s.ProjectTemplateId == templateSection.ProjectTemplateId && s.Id != id) // Exclude self
                .ToListAsync();

            return View(templateSection);
        }

        // POST: TemplateSections/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Order,ProjectTemplateId,ParentSectionId")] TemplateSection templateSection)
        {
            if (id != templateSection.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(templateSection);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TemplateSectionExists(templateSection.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Edit", "ProjectTemplates", new { id = templateSection.ProjectTemplateId });
            }
            return View(templateSection);
        }

        private bool TemplateSectionExists(int id)
        {
            return _context.TemplateSections.Any(e => e.Id == id);
        }

        // POST: TemplateSections/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var templateSection = await _context.TemplateSections.FindAsync(id);
            var projectTemplateId = templateSection.ProjectTemplateId;
            _context.TemplateSections.Remove(templateSection);
            await _context.SaveChangesAsync();
            return RedirectToAction("Edit", "ProjectTemplates", new { id = projectTemplateId });
        }
    }
}