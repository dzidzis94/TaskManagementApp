using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TaskManagementApp.Models;

namespace TaskManagementApp.ViewModels
{
    public class CloneProjectViewModel
    {
        public int SourceProjectId { get; set; }

        public string SourceProjectName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        public string Name { get; set; }

        [StringLength(500, ErrorMessage = "The {0} must be at max {1} characters long.")]
        public string Description { get; set; }

        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
