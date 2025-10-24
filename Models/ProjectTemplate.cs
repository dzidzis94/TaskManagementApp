using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagementApp.Models
{
    /// <summary>
    /// Represents a template for creating new projects with a predefined structure of tasks.
    /// </summary>
    public class ProjectTemplate
    {
        /// <summary>
        /// The unique identifier for the project template.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the project template.
        /// </summary>
        [Required(ErrorMessage = "Template name is required.")]
        [StringLength(100, ErrorMessage = "Template name cannot be longer than 100 characters.")]
        [Display(Name = "Template Name")]
        public string Name { get; set; }

        /// <.summary>
        /// A description of the project template.
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
        [Display(Name = "Description")]
        public string Description { get; set; }

        /// <summary>
        /// The version of the project template.
        /// </summary>
        [StringLength(20, ErrorMessage = "Version cannot be longer than 20 characters.")]
        [Display(Name = "Version")]
        public string Version { get; set; }

        /// <summary>
        /// The date and time when the template was last modified.
        /// </summary>
        [Display(Name = "Last Modified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The collection of sections (task blueprints) that make up this template.
        /// </summary>
        public virtual ICollection<TemplateSection> Sections { get; set; } = new List<TemplateSection>();
    }
}