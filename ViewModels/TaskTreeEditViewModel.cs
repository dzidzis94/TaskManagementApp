using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagementApp.ViewModels
{
    public class TaskTreeEditViewModel
    {
        public int RootTaskId { get; set; }
        public string RootTaskTitle { get; set; }
        public List<TaskEditItem> Tasks { get; set; } = new List<TaskEditItem>();
    }

    public class TaskEditItem
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        public int? ParentTaskId { get; set; }

        public int Depth { get; set; }

        public bool IsDeleted { get; set; }
    }
}
