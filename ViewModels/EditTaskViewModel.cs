using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TaskManagementApp.Models;

namespace TaskManagementApp.ViewModels
{
    public class EditTaskViewModel
    {
        public int Id { get; set; }

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

        [Display(Name = "Status")]
        public TaskStatus Status { get; set; }

        [Display(Name = "Project")]
        public int? ProjectId { get; set; }

        [Display(Name = "Assigned Users")]
        public List<string> SelectedUserIds { get; set; } = new List<string>();
    }
}