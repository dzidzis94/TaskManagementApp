using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementApp.Models
{
    /// <summary>
    /// Represents a single task item within a project.
    /// </summary>
    public class TaskItem
    {
        /// <summary>
        /// The unique identifier for the task.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The title of the task.
        /// </summary>
        [Required(ErrorMessage = "Task title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot be longer than 200 characters.")]
        [Display(Name = "Title")]
        public string Title { get; set; }

        /// <summary>
        /// A detailed description of the task.
        /// </summary>
        [DataType(DataType.MultilineText)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        /// <summary>
        /// The date and time when the task was created.
        /// </summary>
        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The date when the task is due.
        /// </summary>
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// The current status of the task (e.g., Pending, InProgress).
        /// </summary>
        [Display(Name = "Status")]
        public TaskStatus Status { get; set; } = TaskStatus.Pending;

        /// <summary>
        /// The priority level of the task.
        /// </summary>
        [Display(Name = "Priority")]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        /// <summary>
        /// The ID of the parent task, if this is a subtask.
        /// </summary>
        [Display(Name = "Parent Task")]
        public int? ParentTaskId { get; set; }

        /// <summary>
        /// Navigation property for the parent task.
        /// </summary>
        [ForeignKey("ParentTaskId")]
        public virtual TaskItem ParentTask { get; set; }

        /// <summary>
        /// Collection of subtasks for this task.
        /// </summary>
        public virtual ICollection<TaskItem> SubTasks { get; set; } = new List<TaskItem>();

        /// <summary>
        /// The ID of the project this task belongs to.
        /// </summary>
        [Display(Name = "Project")]
        public int? ProjectId { get; set; }

        /// <summary>
        /// Navigation property for the project.
        /// </summary>
        [ForeignKey("ProjectId")]
        public virtual Project Project { get; set; }

        /// <summary>
        /// The ID of the user who created the task.
        /// </summary>
        public string CreatedById { get; set; }

        /// <summary>
        /// Navigation property for the user who created the task.
        /// </summary>
        [ForeignKey("CreatedById")]
        public virtual ApplicationUser CreatedBy { get; set; }

        /// <summary>
        /// The users assigned to this task.
        /// </summary>
        public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();

        /// <summary>
        /// Records of completion for this task.
        /// </summary>
        public virtual ICollection<TaskCompletion> TaskCompletions { get; set; } = new List<TaskCompletion>();
    }

    /// <summary>
    /// Defines the priority levels for a task.
    /// </summary>
    public enum TaskPriority
    {
        [Display(Name = "Low")]
        Low,
        [Display(Name = "Medium")]
        Medium,
        [Display(Name = "High")]
        High
    }

    /// <summary>
    /// Defines the possible statuses for a task.
    /// </summary>
    public enum TaskStatus
    {
        [Display(Name = "Pending")]
        Pending,
        [Display(Name = "In Progress")]
        InProgress,
        [Display(Name = "Completed")]
        Completed,
        [Display(Name = "Cancelled")]
        Cancelled
    }
}