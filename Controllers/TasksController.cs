using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementApp.Data;
using TaskManagementApp.Models;
using TaskManagementApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TaskManagementApp.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TasksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(int? projectId)
        {
            ViewBag.ProjectId = projectId;
            var query = _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.TaskAssignments)
                    .ThenInclude(ta => ta.User)
                .Include(t => t.TaskCompletions)
                .Include(t => t.CreatedBy)
                .Where(t => t.ParentTaskId == null); // Only root tasks

            if (projectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
                var project = await _context.Projects.FindAsync(projectId.Value);
                ViewBag.ProjectName = project?.Name;
            }
            else
            {
                query = query.Where(t => t.ProjectId == null);
                ViewBag.ProjectName = "General Tasks";
            }

            var rootTasks = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();

            // Efficiently load all descendants for the root tasks
            await LoadTaskHierarchy(rootTasks);

            return View(rootTasks);
        }

        private async Task LoadTaskHierarchy(List<TaskItem> rootTasks)
        {
            if (!rootTasks.Any()) return;

            var rootTaskIds = rootTasks.Select(t => t.Id).ToList();

            // Fetch all descendants in a single query
            var allTasks = await _context.Tasks
                .Include(t => t.TaskAssignments).ThenInclude(ta => ta.User)
                .Include(t => t.TaskCompletions)
                .Include(t => t.CreatedBy)
                .ToListAsync();

            var taskDictionary = allTasks.ToDictionary(t => t.Id);

            foreach (var task in allTasks)
            {
                if (task.ParentTaskId.HasValue && taskDictionary.ContainsKey(task.ParentTaskId.Value))
                {
                    var parent = taskDictionary[task.ParentTaskId.Value];
                    // Ensure SubTasks collection is initialized
                    if (parent.SubTasks == null) {
                        parent.SubTasks = new List<TaskItem>();
                    }
                    parent.SubTasks.Add(task);
                }
            }

            // Ensure subtasks are ordered
            foreach(var task in allTasks)
            {
                if(task.SubTasks != null)
                {
                    task.SubTasks = task.SubTasks.OrderByDescending(st => st.CreatedAt).ToList();
                }
            }
        }


        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.ParentTask)
                .Include(t => t.TaskAssignments).ThenInclude(ta => ta.User)
                .Include(t => t.TaskCompletions).ThenInclude(tc => tc.User)
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (task == null) return NotFound();

            // Efficiently load all sub-tasks
            var descendants = new List<TaskItem> { task };
            await LoadTaskHierarchy(descendants);

            return View(task);
        }

        // GET: Tasks/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int? projectId, int? parentTaskId)
        {
            var model = new CreateTaskViewModel
            {
                ProjectId = projectId,
                ParentTaskId = parentTaskId,
                DueDate = DateTime.Today.AddDays(7)
            };

            ViewBag.Users = await _userManager.Users.ToListAsync();
            ViewBag.Projects = await _context.Projects.ToListAsync();

            if (parentTaskId.HasValue)
            {
                var parentTask = await _context.Tasks.FindAsync(parentTaskId.Value);
                if (parentTask != null)
                {
                    ViewBag.ParentTaskTitle = parentTask.Title;
                    model.ProjectId = parentTask.ProjectId; // Inherit project from parent
                }
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
                ViewBag.Projects = await _context.Projects.ToListAsync();
                return View(model);
            }

            var task = new TaskItem
            {
                Title = model.Title,
                Description = model.Description,
                DueDate = model.DueDate,
                ProjectId = model.ProjectId,
                ParentTaskId = model.ParentTaskId,
                CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.UtcNow,
                Status = Models.TaskStatus.Pending,
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync(); // Save to get the new Task Id

            // Handle assignments
            if (model.AssignmentType == "AllUsers")
            {
                var allUsers = await _userManager.Users.ToListAsync();
                foreach (var user in allUsers)
                {
                    _context.TaskAssignments.Add(new TaskAssignment { TaskId = task.Id, UserId = user.Id });
                }
            }
            else if (model.SelectedUserIds != null)
            {
                foreach (var userId in model.SelectedUserIds)
                {
                    _context.TaskAssignments.Add(new TaskAssignment { TaskId = task.Id, UserId = userId });
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Task created successfully!";

            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }

        // GET: Tasks/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var task = await _context.Tasks
                .Include(t => t.TaskAssignments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            var model = new EditTaskViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Status = task.Status,
                ProjectId = task.ProjectId,
                SelectedUserIds = task.TaskAssignments.Select(ta => ta.UserId).ToList()
            };

            ViewBag.Users = await _userManager.Users.ToListAsync();
            ViewBag.Projects = await _context.Projects.ToListAsync();

            return View(model);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, EditTaskViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var task = await _context.Tasks.Include(t => t.TaskAssignments).FirstOrDefaultAsync(t => t.Id == id);
                if (task == null) return NotFound();

                task.Title = model.Title;
                task.Description = model.Description;
                task.DueDate = model.DueDate;
                task.Status = model.Status;
                task.ProjectId = model.ProjectId;

                // Update assignments
                task.TaskAssignments.Clear();
                if (model.SelectedUserIds != null)
                {
                    foreach (var userId in model.SelectedUserIds)
                    {
                        task.TaskAssignments.Add(new TaskAssignment { UserId = userId });
                    }
                }

                try
                {
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Task updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(id)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            ViewBag.Users = await _userManager.Users.ToListAsync();
            ViewBag.Projects = await _context.Projects.ToListAsync();
            return View(model);
        }

        // POST: Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.Include(t => t.SubTasks).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();

            if (task.SubTasks.Any())
            {
                TempData["ErrorMessage"] = "Cannot delete a task that has sub-tasks.";
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Task deleted successfully.";
            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }

        // POST: Tasks/MarkAsCompleted/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            var task = await _context.Tasks
                                .Include(t => t.TaskAssignments)
                                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // User must be assigned to the task to complete it
            if (!task.TaskAssignments.Any(ta => ta.UserId == currentUserId) && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "You are not assigned to this task.";
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            var alreadyCompleted = await _context.TaskCompletions
                .AnyAsync(tc => tc.TaskId == id && tc.UserId == currentUserId);

            if (!alreadyCompleted)
            {
                _context.TaskCompletions.Add(new TaskCompletion
                {
                    TaskId = id,
                    UserId = currentUserId,
                    CompletionDate = DateTime.UtcNow
                });

                // Check if all assigned users have now completed the task
                var totalAssignments = task.TaskAssignments.Count();
                var totalCompletions = await _context.TaskCompletions.CountAsync(tc => tc.TaskId == id) + 1; // +1 for the current one

                if (totalAssignments > 0 && totalCompletions >= totalAssignments)
                {
                    task.Status = Models.TaskStatus.Completed;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Task marked as completed!";
            }
            else
            {
                TempData["InfoMessage"] = "You have already marked this task as completed.";
            }

            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }


        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}