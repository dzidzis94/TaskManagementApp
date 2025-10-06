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
                .Include(t => t.TaskAssignments).ThenInclude(ta => ta.User)
                .Include(t => t.TaskCompletions)
                .Include(t => t.CreatedBy)
                .AsQueryable();

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

            var allTasksInContext = await query.ToListAsync();
            var taskDictionary = allTasksInContext.ToDictionary(t => t.Id);
            var rootTasks = new List<TaskItem>();

            foreach (var task in allTasksInContext)
            {
                if (task.ParentTaskId.HasValue && taskDictionary.TryGetValue(task.ParentTaskId.Value, out var parent))
                {
                    if (parent.SubTasks == null) parent.SubTasks = new List<TaskItem>();
                    parent.SubTasks.Add(task);
                }
                else if (!task.ParentTaskId.HasValue)
                {
                    rootTasks.Add(task);
                }
            }

            // Final ordering pass
            rootTasks = rootTasks.OrderByDescending(t => t.CreatedAt).ToList();
            foreach (var task in allTasksInContext.Where(t => t.SubTasks != null))
            {
                task.SubTasks = task.SubTasks.OrderByDescending(st => st.CreatedAt).ToList();
            }

            return View(rootTasks);
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

            // Fetch all tasks in the same context to build the full hierarchy for the partial views
            var allTasksInContext = await _context.Tasks
                .Where(t => t.ProjectId == task.ProjectId)
                .Include(t => t.TaskAssignments).ThenInclude(ta => ta.User)
                .Include(t => t.TaskCompletions).ThenInclude(tc => tc.User)
                .Include(t => t.CreatedBy)
                .ToListAsync();

            var taskDictionary = allTasksInContext.ToDictionary(t => t.Id);

            foreach (var subTask in allTasksInContext)
            {
                if (subTask.ParentTaskId.HasValue && taskDictionary.TryGetValue(subTask.ParentTaskId.Value, out var parent))
                {
                    if (parent.SubTasks == null) parent.SubTasks = new List<TaskItem>();
                    parent.SubTasks.Add(subTask);
                }
            }

            foreach (var t in allTasksInContext.Where(t => t.SubTasks != null))
            {
                 t.SubTasks = t.SubTasks.OrderByDescending(st => st.CreatedAt).ToList();
            }

            return View(taskDictionary[task.Id]);
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
                    model.ProjectId = parentTask.ProjectId;
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
            await _context.SaveChangesAsync();

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

            if (!ModelState.IsValid)
            {
                ViewBag.Users = await _userManager.Users.ToListAsync();
                ViewBag.Projects = await _context.Projects.ToListAsync();
                return View(model);
            }

            var task = await _context.Tasks.Include(t => t.TaskAssignments).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null) return NotFound();

            task.Title = model.Title;
            task.Description = model.Description;
            task.DueDate = model.DueDate;
            task.Status = model.Status;
            task.ProjectId = model.ProjectId;

            var existingUserIds = task.TaskAssignments.Select(ta => ta.UserId).ToList();
            var selectedUserIds = model.SelectedUserIds ?? new List<string>();
            var userIdsToAdd = selectedUserIds.Except(existingUserIds).ToList();
            var assignmentsToRemove = task.TaskAssignments.Where(ta => !selectedUserIds.Contains(ta.UserId)).ToList();

            _context.TaskAssignments.RemoveRange(assignmentsToRemove);
            foreach (var userId in userIdsToAdd)
            {
                task.TaskAssignments.Add(new TaskAssignment { TaskId = task.Id, UserId = userId });
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

            if (!task.TaskAssignments.Any(ta => ta.UserId == currentUserId) && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "You are not assigned to this task.";
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            var alreadyCompleted = await _context.TaskCompletions
                .AnyAsync(tc => tc.TaskId == id && tc.UserId == currentUserId);

            if (alreadyCompleted)
            {
                TempData["InfoMessage"] = "You have already marked this task as completed.";
                return RedirectToAction("Index", new { projectId = task.ProjectId });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.TaskCompletions.Add(new TaskCompletion
                    {
                        TaskId = id,
                        UserId = currentUserId,
                        CompletionDate = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();

                    var totalAssignments = task.TaskAssignments.Count();
                    var totalCompletions = await _context.TaskCompletions.CountAsync(tc => tc.TaskId == id);

                    if (totalAssignments > 0 && totalCompletions >= totalAssignments)
                    {
                        task.Status = Models.TaskStatus.Completed;
                        _context.Update(task);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Task marked as completed!";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "An error occurred while completing the task.";
                }
            }
            return RedirectToAction("Index", new { projectId = task.ProjectId });
        }


        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}