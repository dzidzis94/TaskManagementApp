using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementApp.Models
{
    /// <summary>
    /// Represents a section within a project template, which acts as a blueprint for a TaskItem.
    /// </summary>
    public class TemplateSection
    {
        /// <summary>
        /// The unique identifier for the template section.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The title for tasks created from this section.
        /// </summary>
        [Required(ErrorMessage = "Section title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters.")]
        [Display(Name = "Title")]
        public string Title { get; set; }

        /// <summary>
        /// The description for tasks created from this section.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description cannot be longer than 1000 characters.")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        /// <summary>
        /// The default priority for tasks created from this section.
        /// </summary>
        [Display(Name = "Priority")]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        /// <summary>
        /// The number of days from project creation this task should be due.
        /// A null value means no due date will be set.
        /// </summary>
        [Display(Name = "Due Date Offset (Days)")]
        public int? DueDateOffsetDays { get; set; }

        /// <summary>
        /// The display order of this section within its parent.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The ID of the project template this section belongs to.
        /// </summary>
        public int ProjectTemplateId { get; set; }

        /// <summary>
        /// Navigation property for the project template.
        /// </summary>
        [ForeignKey("ProjectTemplateId")]
        public virtual ProjectTemplate ProjectTemplate { get; set; }

        /// <summary>
        /// The ID of the parent section, if this is a nested section.
        /// </summary>
        public int? ParentSectionId { get; set; }

        /// <summary>
        /// Navigation property for the parent section.
        /// </summary>
        [ForeignKey("ParentSectionId")]
        public virtual TemplateSection ParentSection { get; set; }

        /// <summary>
        /// Collection of child sections nested under this one.
        /// </summary>
        public virtual ICollection<TemplateSection> ChildSections { get; set; } = new List<TemplateSection>();
    }
}