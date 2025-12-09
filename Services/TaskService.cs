using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManagementApp.Data;
using TaskManagementApp.Models;
using TaskManagementApp.Services.Interfaces;
using TaskManagementApp.ViewModels;

namespace TaskManagementApp.Services
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<TaskService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IEnumerable<TaskSummaryViewModel>> GetTasksAsync(int? projectId)
        {
            var query = _context.Tasks.AsQueryable();

            if (projectId.HasValue)
            {
                query = query.Where(t => t.ProjectId == projectId.Value);
            }
            else
            {
                query = query.Where(t => t.ProjectId == null);
            }

            var flatList = await query
                .Select(t => new TaskSummaryViewModel
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    ProjectId = t.ProjectId,
                    ParentTaskId = t.ParentTaskId,
                    CreatedAt = t.CreatedAt,
                    AssignedUserNames = t.TaskAssignments.Select(ta => ta.User.UserName).ToList(),
                    AssignedUserIds = t.TaskAssignments.Select(ta => ta.UserId).ToList(),
                    CompletedUserNames = t.TaskCompletions.Select(tc => tc.User.UserName).ToList(),
                    CompletedUserIds = t.TaskCompletions.Select(tc => tc.UserId).ToList()
                })
                .ToListAsync();

            var taskDictionary = flatList.ToDictionary(t => t.Id);
            var rootTasks = new List<TaskSummaryViewModel>();

            foreach (var task in flatList)
            {
                if (task.ParentTaskId.HasValue && taskDictionary.TryGetValue(task.ParentTaskId.Value, out var parent))
                {
                    parent.SubTasks.Add(task);
                }
                else if (!task.ParentTaskId.HasValue)
                {
                    rootTasks.Add(task);
                }
            }

            foreach (var task in flatList.Where(t => t.SubTasks.Any()))
            {
                task.SubTasks = task.SubTasks.OrderByDescending(st => st.CreatedAt).ToList();
            }

            return rootTasks.OrderByDescending(t => t.CreatedAt).ToList();
        }

        public async Task<TaskItem> GetTaskByIdAsync(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.ParentTask)
                .Include(t => t.TaskAssignments).ThenInclude(ta => ta.User)
                .Include(t => t.TaskCompletions).ThenInclude(tc => tc.User)
                .Include(t => t.CreatedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (task == null) return null;

            // Build the full hierarchy for the task
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
                    parent.SubTasks.Add(subTask);
                }
            }

            return taskDictionary[task.Id];
        }

        public async Task<TaskItem> CreateTaskAsync(CreateTaskViewModel model, string createdById)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var task = new TaskItem
                    {
                        Title = model.Title,
                        Description = model.Description,
                        DueDate = model.DueDate,
                        ProjectId = model.ProjectId,
                        ParentTaskId = model.ParentTaskId,
                        CreatedById = createdById,
                        CreatedAt = DateTime.UtcNow,
                        Status = Models.TaskStatus.Pending,
                    };

                    _context.Tasks.Add(task);
                    await _context.SaveChangesAsync(); // Save to get the new task ID

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
                    await transaction.CommitAsync();
                    return task;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task UpdateTaskAsync(EditTaskViewModel model)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var task = await _context.Tasks.Include(t => t.TaskAssignments).FirstOrDefaultAsync(t => t.Id == model.Id);
                    if (task == null)
                    {
                        throw new KeyNotFoundException("Task not found.");
                    }

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

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            var task = await _context.Tasks.Include(t => t.SubTasks).FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
            {
                return false; // Task not found
            }

            if (task.SubTasks.Any())
            {
                throw new InvalidOperationException("Cannot delete task because it has existing sub-tasks.");
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TaskCompletionResult> MarkTaskAsCompletedAsync(int taskId, string userId, bool isAdmin)
        {
            var task = await _context.Tasks
                .Include(t => t.TaskAssignments)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                return new TaskCompletionResult { Success = false, Message = "Task not found." };
            }

            if (!task.TaskAssignments.Any(ta => ta.UserId == userId) && !isAdmin)
            {
                return new TaskCompletionResult { Success = false, Message = "You are not assigned to this task.", ProjectId = task.ProjectId };
            }

            if (await _context.TaskCompletions.AnyAsync(tc => tc.TaskId == taskId && tc.UserId == userId))
            {
                return new TaskCompletionResult { Success = false, Message = "You have already marked this task as completed.", ProjectId = task.ProjectId };
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.TaskCompletions.Add(new TaskCompletion { TaskId = taskId, UserId = userId, CompletionDate = DateTime.UtcNow });
                    await _context.SaveChangesAsync();

                    var totalAssignments = task.TaskAssignments.Count();
                    var totalCompletions = await _context.TaskCompletions.CountAsync(tc => tc.TaskId == taskId);

                    if (totalAssignments > 0 && totalCompletions >= totalAssignments)
                    {
                        task.Status = Models.TaskStatus.Completed;
                        _context.Update(task);
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                    return new TaskCompletionResult { Success = true, Message = "Task marked as completed!", ProjectId = task.ProjectId };
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error occurred while completing task {TaskId}", taskId);
                    return new TaskCompletionResult { Success = false, Message = "An error occurred while completing the task.", ProjectId = task.ProjectId };
                }
            }
        }

        public async Task<CreateTaskViewModel> GetCreateTaskViewModelAsync(int? projectId, int? parentTaskId)
        {
            var model = new CreateTaskViewModel
            {
                ProjectId = projectId,
                ParentTaskId = parentTaskId,
                DueDate = DateTime.Today.AddDays(7)
            };

            if (parentTaskId.HasValue)
            {
                var parentTask = await _context.Tasks.FindAsync(parentTaskId.Value);
                if (parentTask != null)
                {
                    model.ProjectId = parentTask.ProjectId;
                }
            }

            return model;
        }

        public async Task<EditTaskViewModel> GetEditTaskViewModelAsync(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.TaskAssignments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return null;

            return new EditTaskViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Status = task.Status,
                ProjectId = task.ProjectId,
                SelectedUserIds = task.TaskAssignments.Select(ta => ta.UserId).ToList()
            };
        }
    }
}