using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManagementApp.ViewModels
{
    public class CreateTaskViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [Display(Name = "Title")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Project")]
        public int? ProjectId { get; set; }

        [Display(Name = "Parent Task")]
        public int? ParentTaskId { get; set; }

        [Display(Name = "Assignment Type")]
        public string AssignmentType { get; set; } = "SpecificUsers";

        [Display(Name = "Assigned Users")]
        public List<string> SelectedUserIds { get; set; } = new List<string>();
    }
}