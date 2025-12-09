using System;
using System.Collections.Generic;
using TaskManagementApp.Models;

namespace TaskManagementApp.ViewModels
{
    public class TaskSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public int? ProjectId { get; set; }
        public int? ParentTaskId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> AssignedUserNames { get; set; } = new List<string>();
        public List<string> AssignedUserIds { get; set; } = new List<string>();
        public List<string> CompletedUserNames { get; set; } = new List<string>();
        public List<string> CompletedUserIds { get; set; } = new List<string>();
        public List<TaskSummaryViewModel> SubTasks { get; set; } = new List<TaskSummaryViewModel>();
    }
}
