using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagementApp.Models
{
    /// <summary>
    /// Represents a project, which is a container for tasks.
    /// </summary>
    public class Project
    {
        /// <summary>
        /// The unique identifier for the project.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the project.
        /// </summary>
        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(100, ErrorMessage = "Project name cannot be longer than 100 characters.")]
        [Display(Name = "Project Name")]
        public string Name { get; set; }

        /// <summary>
        /// A description of the project.
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
        public string Description { get; set; }

        /// <summary>
        /// Indicates whether the project is visible to everyone or only to assigned users.
        /// </summary>
        [Display(Name = "Public Project")]
        public bool IsPublic { get; set; } = true;

        /// <summary>
        /// The collection of tasks associated with this project.
        /// </summary>
        public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}