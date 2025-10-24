using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TaskManagementApp.Models;
using TaskManagementApp.Services.Interfaces;

namespace TaskManagementApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        // GET: Projects
        public async Task<IActionResult> Index()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            return View(projects);
        }

        // GET: Projects/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _projectService.GetProjectByIdAsync(id.Value);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        // GET: Projects/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.ProjectTemplates = await _projectService.GetAllProjectTemplatesAsync();
            return View();
        }

        // POST: Projects/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,IsPublic")] Project project, int? templateId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (templateId.HasValue)
                    {
                        await _projectService.CreateProjectFromTemplateAsync(templateId.Value, project.Name, project.Description);
                    }
                    else
                    {
                        await _projectService.CreateProjectAsync(project);
                    }
                    TempData["SuccessMessage"] = "Project created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating project.");
                    ModelState.AddModelError("", "An unexpected error occurred while creating the project.");
                }
            }
            ViewBag.ProjectTemplates = await _projectService.GetAllProjectTemplatesAsync();
            return View(project);
        }

        // GET: Projects/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _projectService.GetProjectByIdAsync(id.Value);
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
                    await _projectService.UpdateProjectAsync(project);
                    TempData["SuccessMessage"] = "Project updated successfully!";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating project {ProjectId}", project.Id);
                    if (!await _projectService.ProjectExistsAsync(project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError("", "An unexpected error occurred while updating the project.");
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
            try
            {
                await _projectService.DeleteProjectAsync(id);
                TempData["SuccessMessage"] = "Project and all associated data have been deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project {ProjectId}", id);
                TempData["ErrorMessage"] = "An error occurred during deletion.";
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Projects/DeleteMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple(int[] projectIds)
        {
            if (projectIds == null || projectIds.Length == 0)
            {
                TempData["ErrorMessage"] = "No projects selected for deletion.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _projectService.DeleteMultipleProjectsAsync(projectIds);
                TempData["SuccessMessage"] = $"{projectIds.Length} project(s) and all their associated data have been deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk deletion of projects.");
                TempData["ErrorMessage"] = "An error occurred during bulk deletion.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Projects/GetTemplatePreview/5
        [HttpGet]
        public async Task<IActionResult> GetTemplatePreview(int id)
        {
            var preview = await _projectService.GetTemplatePreviewAsync(id);
            if (preview == null)
            {
                return NotFound();
            }
            return Json(preview);
        }
    }
}