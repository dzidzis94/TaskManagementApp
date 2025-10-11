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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [FromBody] TemplateSection templateSection)
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
                return Ok();
            }
            return BadRequest(ModelState);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] TemplateSection templateSection)
        {
            if (ModelState.IsValid)
            {
                _context.Add(templateSection);
                await _context.SaveChangesAsync();
                return PartialView("ProjectTemplates/_SectionEditorRow", templateSection);
            }
            return BadRequest(ModelState);
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
            var templateSection = await _context.TemplateSections
                .Include(s => s.ChildSections)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (templateSection == null)
            {
                return NotFound();
            }

            var projectTemplateId = templateSection.ProjectTemplateId;

            var sectionsToDelete = new List<TemplateSection>();
            FindSectionsToDelete(templateSection, sectionsToDelete);

            _context.TemplateSections.RemoveRange(sectionsToDelete);
            await _context.SaveChangesAsync();

            return RedirectToAction("Edit", "ProjectTemplates", new { id = projectTemplateId });
        }

        private void FindSectionsToDelete(TemplateSection section, List<TemplateSection> sectionsToDelete)
        {
            sectionsToDelete.Add(section);
            foreach (var child in section.ChildSections)
            {
                FindSectionsToDelete(child, sectionsToDelete);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStructure(int projectTemplateId, [FromBody] List<TemplateSection> sections)
        {
            var existingSections = await _context.TemplateSections
                .Where(s => s.ProjectTemplateId == projectTemplateId)
                .ToListAsync();

            foreach (var section in sections)
            {
                var existingSection = existingSections.FirstOrDefault(s => s.Id == section.Id);
                if (existingSection != null)
                {
                    existingSection.ParentSectionId = section.ParentSectionId;
                    existingSection.Order = section.Order;
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}