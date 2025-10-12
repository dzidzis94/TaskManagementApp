using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskManagementApp.Models;
using TaskManagementApp.Services.Interfaces;
using TaskManagementApp.ViewModels;

namespace TaskManagementApp.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            ITaskService taskService,
            IProjectService projectService,
            UserManager<ApplicationUser> userManager,
            ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _projectService = projectService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(int? projectId)
        {
            ViewBag.ProjectId = projectId;
            if (projectId.HasValue)
            {
                var project = await _projectService.GetProjectByIdAsync(projectId.Value);
                ViewBag.ProjectName = project?.Name ?? "Project Not Found";
            }
            else
            {
                ViewBag.ProjectName = "General Tasks";
            }

            var tasks = await _taskService.GetTasksAsync(projectId);
            return View(tasks);
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _taskService.GetTaskByIdAsync(id.Value);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // GET: Tasks/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int? projectId, int? parentTaskId)
        {
            var model = await _taskService.GetCreateTaskViewModelAsync(projectId, parentTaskId);

            ViewBag.Users = await _userManager.Users.ToListAsync();
            ViewBag.Projects = await _projectService.GetAllProjectsAsync();

            if (parentTaskId.HasValue)
            {
                var parentTask = await _taskService.GetTaskByIdAsync(parentTaskId.Value);
                ViewBag.ParentTaskTitle = parentTask?.Title;
            }

            return View(model);
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Users = await _userManager.Users.ToListAsync();
                ViewBag.Projects = await _projectService.GetAllProjectsAsync();
                return View(model);
            }

            try
            {
                var createdById = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var task = await _taskService.CreateTaskAsync(model, createdById);
                TempData["SuccessMessage"] = "Task created successfully!";
                return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task.");
                ModelState.AddModelError("", "An unexpected error occurred while creating the task.");
                ViewBag.Users = await _userManager.Users.ToListAsync();
                ViewBag.Projects = await _projectService.GetAllProjectsAsync();
                return View(model);
            }
        }

        // GET: Tasks/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var model = await _taskService.GetEditTaskViewModelAsync(id.Value);
            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Users = await _userManager.Users.ToListAsync();
            ViewBag.Projects = await _projectService.GetAllProjectsAsync();
            return View(model);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, EditTaskViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Users = await _userManager.Users.ToListAsync();
                ViewBag.Projects = await _projectService.GetAllProjectsAsync();
                return View(model);
            }

            try
            {
                await _taskService.UpdateTaskAsync(model);
                TempData["SuccessMessage"] = "Task updated successfully!";
                return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task {TaskId}", model.Id);
                ModelState.AddModelError("", "An unexpected error occurred while updating the task.");
                ViewBag.Users = await _userManager.Users.ToListAsync();
                ViewBag.Projects = await _projectService.GetAllProjectsAsync();
                return View(model);
            }
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deletedTask = await _taskService.DeleteTaskAsync(id);
            if (deletedTask != null) // This means deletion failed because of sub-tasks
            {
                TempData["ErrorMessage"] = "Cannot delete a task that has sub-tasks.";
                return RedirectToAction(nameof(Index), new { projectId = deletedTask.ProjectId });
            }

            TempData["SuccessMessage"] = "Task deleted successfully.";
            // Since we don't know the project ID after deletion, redirect to general tasks
            return RedirectToAction(nameof(Index));
        }

        // POST: Tasks/MarkAsCompleted/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var result = await _taskService.MarkTaskAsCompletedAsync(id, currentUserId, isAdmin);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction(nameof(Index), new { projectId = result.ProjectId });
        }
    }
}