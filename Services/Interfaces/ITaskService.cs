using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagementApp.Models;
using TaskManagementApp.ViewModels;

namespace TaskManagementApp.Services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages task-related operations.
    /// </summary>
    public interface ITaskService
    {
        /// <summary>
        /// Retrieves a hierarchical list of root tasks for a specific project or general tasks.
        /// </summary>
        /// <param name="projectId">The optional ID of the project to filter by.</param>
        /// <returns>A collection of root-level task items with their sub-tasks populated.</returns>
        Task<IEnumerable<TaskSummaryViewModel>> GetTasksAsync(int? projectId);

        /// <summary>
        /// Retrieves a single task by its ID, with its full hierarchy.
        /// </summary>
        /// <param name="id">The ID of the task to retrieve.</param>
        /// <returns>The task with the specified ID, or null if not found.</returns>
        Task<TaskItem?> GetTaskByIdAsync(int id);

        /// <summary>
        /// Creates a new task based on the provided view model.
        /// </summary>
        /// <param name="model">The view model containing the data for the new task.</param>
        /// <param name="createdById">The ID of the user creating the task.</param>
        Task<TaskItem> CreateTaskAsync(CreateTaskViewModel model, string createdById);

        /// <summary>
        /// Updates an existing task based on the provided view model.
        /// </summary>
        /// <param name="model">The view model containing the updated task data.</param>
        Task UpdateTaskAsync(EditTaskViewModel model);

        /// <summary>
        /// Deletes a task.
        /// </summary>
        /// <param name="id">The ID of the task to delete.</param>
        /// <returns>The deleted task item, or null if not found or if it has sub-tasks.</returns>
        Task<bool> DeleteTaskAsync(int id);

        /// <summary>
        /// Marks a task as completed for the specified user.
        /// </summary>
        /// <param name="taskId">The ID of the task to mark as completed.</param>
        /// <param name="userId">The ID of the user completing the task.</param>
        /// <param name="isAdmin">A flag indicating if the user is an administrator.</param>
        /// <returns>A service result indicating the outcome of the operation.</returns>
        Task<TaskCompletionResult> MarkTaskAsCompletedAsync(int taskId, string userId, bool isAdmin);

        /// <summary>
        /// Retrieves the necessary data for the create task view.
        /// </summary>
        /// <param name="projectId">Optional project ID.</param>
        /// <param name="parentTaskId">Optional parent task ID.</param>
        /// <returns>A view model for creating a task.</returns>
        Task<CreateTaskViewModel> GetCreateTaskViewModelAsync(int? projectId, int? parentTaskId);

        /// <summary>
        /// Retrieves the necessary data for the edit task view.
        /// </summary>
        /// <param name="id">The ID of the task to edit.</param>
        /// <returns>A view model for editing a task, or null if the task is not found.</returns>
        Task<EditTaskViewModel?> GetEditTaskViewModelAsync(int id);

        /// <summary>
        /// Deep clones a task and its sub-tasks.
        /// </summary>
        /// <param name="sourceTaskId">The ID of the task to clone.</param>
        /// <param name="targetProjectId">Optional target project ID. If null, the source project ID is used.</param>
        /// <param name="userId">The ID of the user performing the clone.</param>
        /// <param name="newParentId">Optional new parent task ID for the cloned task.</param>
        /// <returns>The ID of the newly created task.</returns>
        Task<int> CloneTaskAsync(int sourceTaskId, int? targetProjectId, string userId, int? newParentId = null);
    }

    public class TaskCompletionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? ProjectId { get; set; }
    }
}