using System.Collections.Generic;
using TaskManagementApp.Models;

namespace TaskManagementApp.ViewModels
{
    public class DashboardViewModel
    {
        public List<ActivityItem> RecentActivity { get; set; } = new List<ActivityItem>();
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public double CompletionPercentage { get; set; }
        public List<TaskItem> MyTasks { get; set; } = new List<TaskItem>();
    }

    public class ActivityItem
    {
        public string Message { get; set; }
        public System.DateTime Timestamp { get; set; }
        public string UserFullName { get; set; }
        public string ActivityType { get; set; } // e.g., "New Task", "Task Completed"
        public int? RelatedId { get; set; } // e.g., TaskId
    }
}