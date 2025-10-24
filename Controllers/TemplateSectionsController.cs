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

        // POST: api/templates/sections
        [HttpPost("api/templates/sections")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,ProjectTemplateId,ParentSectionId")] TemplateSection templateSection)
        {
            if (ModelState.IsValid)
            {
                _context.Add(templateSection);
                await _context.SaveChangesAsync();
                return Ok(new { id = templateSection.Id });
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

        // POST: api/templates/sections/5
        [HttpPost("api/templates/sections/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string title, string description)
        {
            var sectionToUpdate = await _context.TemplateSections.FindAsync(id);

            if (sectionToUpdate == null)
            {
                return NotFound();
            }

            sectionToUpdate.Title = title;
            sectionToUpdate.Description = description;
            _context.Update(sectionToUpdate);
            await _context.SaveChangesAsync();
            return Ok();
        }

        private bool TemplateSectionExists(int id)
        {
            return _context.TemplateSections.Any(e => e.Id == id);
        }

        // POST: api/templates/sections/delete/5
        [HttpPost("api/templates/sections/delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var templateSection = await _context.TemplateSections
                .Include(s => s.ChildSections)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (templateSection == null)
            {
                return NotFound();
            }

            var sectionsToDelete = new List<TemplateSection>();
            FindSectionsToDelete(templateSection, sectionsToDelete);

            _context.TemplateSections.RemoveRange(sectionsToDelete);
            await _context.SaveChangesAsync();

            return Ok();
        }

        private void FindSectionsToDelete(TemplateSection section, List<TemplateSection> sectionsToDelete)
        {
            sectionsToDelete.Add(section);
            foreach (var child in section.ChildSections)
            {
                FindSectionsToDelete(child, sectionsToDelete);
            }
        }

        [HttpPost("api/templates/sections/update-structure")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStructure(int projectTemplateId, [FromBody] List<TemplateSection> sections)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sectionIds = sections.Select(s => s.Id).ToList();
            var existingSections = await _context.TemplateSections
                .Where(s => s.ProjectTemplateId == projectTemplateId && sectionIds.Contains(s.Id))
                .ToListAsync();

            var sectionMap = existingSections.ToDictionary(s => s.Id);

            foreach (var sectionData in sections)
            {
                if (sectionMap.TryGetValue(sectionData.Id, out var existingSection))
                {
                    existingSection.ParentSectionId = sectionData.ParentSectionId;
                    existingSection.Order = sectionData.Order;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                // Log the error
                return StatusCode(500, "A database error occurred while updating the template structure.");
            }
        }
    }
}