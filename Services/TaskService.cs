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
                    Priority = t.Priority,
                    DueDate = t.DueDate,
                    ProjectId = t.ProjectId,
                    ParentTaskId = t.ParentTaskId,
                    CreatedAt = t.CreatedAt,
                    AssignedUserNames = t.TaskAssignments.Where(ta => ta.User != null).Select(ta => ta.User.UserName).ToList()!,
                    AssignedUserIds = t.TaskAssignments.Select(ta => ta.UserId).ToList(),
                    CompletedUserNames = t.TaskCompletions.Where(tc => tc.User != null).Select(tc => tc.User.UserName).ToList()!,
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

            // Calculate CompletionPercentage for root tasks
            foreach (var rootTask in rootTasks)
            {
                var (completed, total) = CalculateTaskCompletion(rootTask);
                rootTask.CompletionPercentage = total > 0 ? (double)completed / total * 100 : 0;
            }

            return rootTasks.OrderByDescending(t => t.CreatedAt).ToList();
        }

        private (int completed, int total) CalculateTaskCompletion(TaskSummaryViewModel task)
        {
            int completed = task.Status == Models.TaskStatus.Completed ? 1 : 0;
            int total = 1;

            if (task.SubTasks != null)
            {
                foreach (var subTask in task.SubTasks)
                {
                    var (subCompleted, subTotal) = CalculateTaskCompletion(subTask);
                    completed += subCompleted;
                    total += subTotal;
                }
            }

            return (completed, total);
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id)
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

        public async Task<EditTaskViewModel?> GetEditTaskViewModelAsync(int id)
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

        public async Task<int> CloneTaskTreeAsync(int sourceTaskId, int? targetProjectId, string userId, int? newParentId = null, List<int>? excludedTaskIds = null)
        {
            // A. Load the entire tree with AsNoTracking (Critical for breaking references)
            var sourceTask = await _context.Tasks
                .AsNoTracking()
                .Include(t => t.SubTasks)
                .Include(t => t.SubTasks).ThenInclude(st => st.SubTasks) // Support 3 levels deep
                .Include(t => t.SubTasks).ThenInclude(st => st.SubTasks).ThenInclude(sst => sst.SubTasks) // Support 4 levels deep
                .FirstOrDefaultAsync(t => t.Id == sourceTaskId);

            if (sourceTask == null) throw new ArgumentException("Source task not found");

            // B. Map to new structure in memory (Recursive)
            var newTaskRoot = MapTaskRecursive(sourceTask, targetProjectId, userId, excludedTaskIds);

            if (newTaskRoot == null) return -1;

            // C. Set the top-level parent if this is a sub-branch clone
            if (newParentId.HasValue) { newTaskRoot.ParentTaskId = newParentId; }

            // D. Batch Save (One transaction for the whole tree)
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    _context.Tasks.Add(newTaskRoot);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return newTaskRoot.Id;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        private TaskItem? MapTaskRecursive(TaskItem source, int? targetProjectId, string userId, List<int>? excludedTaskIds)
        {
            if (excludedTaskIds != null && excludedTaskIds.Contains(source.Id))
            {
                return null;
            }

            var newTask = new TaskItem
            {
                // Copy Data
                Title = source.Title,
                Description = source.Description,
                Priority = source.Priority,
                DueDate = null, // Reset dates for clones

                // Reset Metadata
                Id = 0, // Force EF to insert as new
                ProjectId = targetProjectId ?? source.ProjectId,
                CreatedById = userId,
                CreatedAt = DateTime.UtcNow,
                Status = Models.TaskStatus.Pending, // Always start as Pending/Draft

                // Clean Relations
                ParentTaskId = null, // Will be handled by EF via SubTasks collection
                TaskAssignments = new List<TaskAssignment>(),
                TaskCompletions = new List<TaskCompletion>(),
                SubTasks = new List<TaskItem>()
            };

            // Recursively map children
            if (source.SubTasks != null && source.SubTasks.Any())
            {
                foreach (var child in source.SubTasks)
                {
                    var newChild = MapTaskRecursive(child, targetProjectId, userId, excludedTaskIds);
                    if (newChild != null)
                    {
                        newTask.SubTasks.Add(newChild);
                    }
                }
            }
            return newTask;
        }

        public async Task<TaskTreeEditViewModel?> GetTaskTreeForEditAsync(int rootTaskId)
        {
            var rootTask = await GetTaskByIdAsync(rootTaskId);
            if (rootTask == null) return null;

            var model = new TaskTreeEditViewModel
            {
                RootTaskId = rootTask.Id,
                RootTaskTitle = rootTask.Title,
                Tasks = new List<TaskEditItem>()
            };

            FlattenTaskTree(rootTask, model.Tasks, 0);

            return model;
        }

        private void FlattenTaskTree(TaskItem task, List<TaskEditItem> flatList, int depth)
        {
            flatList.Add(new TaskEditItem
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                ParentTaskId = task.ParentTaskId,
                Depth = depth,
                IsDeleted = false
            });

            if (task.SubTasks != null)
            {
                foreach (var subTask in task.SubTasks)
                {
                    FlattenTaskTree(subTask, flatList, depth + 1);
                }
            }
        }

        public async Task UpdateTaskTreeAsync(TaskTreeEditViewModel model)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Fetch the root task with full hierarchy (tracked)
                    var rootTask = await GetTaskByIdAsync(model.RootTaskId);
                    if (rootTask == null) throw new KeyNotFoundException("Root task not found.");

                    // Flatten existing tree to map by ID
                    var existingTasks = new Dictionary<int, TaskItem>();
                    FlattenTaskTreeToDictionary(rootTask, existingTasks);

                    // Mēs paņemam tikai tos, kuri NAV atzīmēti kā dzēšami.
                    // Tie, kas IR atzīmēti (IsDeleted=true), netiks iekļauti šajā sarakstā,
                    // un nākamais koda bloks (tasksToDelete) tos automātiski izdzēsīs no datubāzes.
                    var submittedTaskIds = model.Tasks
                        .Where(t => !t.IsDeleted)
                        .Select(t => t.Id)
                        .ToHashSet();
                    // Delete tasks that are in DB but missing from submission.
                    var tasksToDelete = existingTasks.Values
                        .Where(t => !submittedTaskIds.Contains(t.Id) && t.Id != model.RootTaskId)
                        .ToList();

                    // Also identify orphans in the submission (tasks whose parents are being deleted)
                    var idsToDelete = tasksToDelete.Select(t => t.Id).ToHashSet();
                    bool added;
                    do
                    {
                        added = false;
                        foreach (var item in model.Tasks)
                        {
                            if (!idsToDelete.Contains(item.Id) && item.ParentTaskId.HasValue && idsToDelete.Contains(item.ParentTaskId.Value))
                            {
                                idsToDelete.Add(item.Id);
                                if (existingTasks.TryGetValue(item.Id, out var existingTask))
                                {
                                    tasksToDelete.Add(existingTask);
                                }
                                added = true;
                            }
                        }
                    } while (added);

                    if (tasksToDelete.Any())
                    {
                        _context.Tasks.RemoveRange(tasksToDelete);
                    }

                    // Update existing tasks (skip those marked for deletion)
                    foreach (var item in model.Tasks)
                    {
                        if (idsToDelete.Contains(item.Id)) continue;

                        if (existingTasks.TryGetValue(item.Id, out var existingTask))
                        {
                            existingTask.Title = item.Title;
                            existingTask.Description = item.Description;
                            _context.Entry(existingTask).State = EntityState.Modified;
                        }
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

        private void FlattenTaskTreeToDictionary(TaskItem task, Dictionary<int, TaskItem> dictionary)
        {
            dictionary[task.Id] = task;
            if (task.SubTasks != null)
            {
                foreach (var subTask in task.SubTasks)
                {
                    FlattenTaskTreeToDictionary(subTask, dictionary);
                }
            }
        }
    }
}