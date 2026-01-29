using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskManagementApp.Models;
using TaskManagementApp.Services.Interfaces;
using TaskManagementApp.ViewModels;
using TaskManagementApp.Helpers;

namespace TaskManagementApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProjectsController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly ITemplateService _templateService;

        // 1. IZMAIŅA: Pievienojam TaskService, lai varētu ielādēt uzdevumus jaunajā formātā
        private readonly ITaskService _taskService;
        private readonly ILogger<ProjectsController> _logger;

        // 2. IZMAIŅA: Pievienojam taskService konstruktorā
        public ProjectsController(
            IProjectService projectService,
            ITemplateService templateService,
            ITaskService taskService,
            ILogger<ProjectsController> logger)
        {
            _projectService = projectService;
            _templateService = templateService;
            _taskService = taskService;
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

            // 3. IZMAIŅA: Šis salabo tavu kļūdu!
            // Mēs palūdzam TaskService sagatavot datus (ar statusiem, krāsām un hierarhiju),
            // un ieliekam tos ViewBag, ko gaida tavs skats.
            ViewBag.ProjectTasks = await _taskService.GetTasksAsync(project.Id);

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

        // GET: Projects/Clone/5
        public async Task<IActionResult> Clone(int? id)
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

            var viewModel = new CloneProjectViewModel
            {
                SourceProjectId = project.Id,
                SourceProjectName = project.Name,
                Name = $"Clone of {project.Name}",
                Description = project.Description,
                Tasks = HierarchyHelper.BuildTaskHierarchy(project.Tasks)
            };

            return View(viewModel);
        }

        // POST: Projects/Clone/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clone(int id, [FromForm] CloneProjectViewModel viewModel, [FromForm] System.Collections.Generic.List<int> excludedTaskIds)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var newProject = await _templateService.CloneProjectAsync(id, viewModel.Name, viewModel.Description, userId, excludedTaskIds);
                    TempData["SuccessMessage"] = "Project cloned successfully!";
                    return RedirectToAction(nameof(Details), new { id = newProject.Id });
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error cloning project.");
                    ModelState.AddModelError("", "An unexpected error occurred while cloning the project.");
                }
            }

            var sourceProject = await _projectService.GetProjectByIdAsync(id);
            if (sourceProject == null)
            {
                return NotFound();
            }
            viewModel.SourceProjectName = sourceProject.Name;
            viewModel.Tasks = HierarchyHelper.BuildTaskHierarchy(sourceProject.Tasks);

            return View(viewModel);
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