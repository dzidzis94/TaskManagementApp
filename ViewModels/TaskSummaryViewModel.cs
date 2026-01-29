using System;
using System.Collections.Generic;
using TaskManagementApp.Models;

using ModelTaskStatus = TaskManagementApp.Models.TaskStatus;

namespace TaskManagementApp.ViewModels
{
    public class TaskSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ModelTaskStatus Status { get; set; }
        public TaskPriority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int? ParentTaskId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByUserName { get; set; }
        public List<string> AssignedUserNames { get; set; } = new List<string>();
        public List<string> AssignedUserIds { get; set; } = new List<string>();
        public List<string> CompletedUserNames { get; set; } = new List<string>();
        public List<string> CompletedUserIds { get; set; } = new List<string>();
        public List<TaskCompletionViewModel> Completions { get; set; } = new List<TaskCompletionViewModel>();
        public double CompletionPercentage { get; set; }
        public List<TaskSummaryViewModel> SubTasks { get; set; } = new List<TaskSummaryViewModel>();
    }

    public class TaskCompletionViewModel
    {
        public string UserName { get; set; }
        public DateTime CompletionDate { get; set; }
    }
}
