using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace TaskManagementApp.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsPublic { get; set; } = true;

        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}