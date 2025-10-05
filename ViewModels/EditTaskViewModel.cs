using System.ComponentModel.DataAnnotations;
using TaskManagementApp.Models;

namespace TaskManagementApp.ViewModels
{
    public class EditTaskViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nosaukums ir obligāts")]
        [Display(Name = "Nosaukums")]
        [StringLength(200, ErrorMessage = "Nosaukums nedrīkst pārsniegt 200 rakstzīmes")]
        public string Title { get; set; }

        [Display(Name = "Apraksts")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Termiņš")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Statuss")]
        public Models.TaskStatus Status { get; set; }

        [Display(Name = "Piešķirtais lietotājs")]
        public string AssignedUserId { get; set; }

        [Display(Name = "Vecākuzdevums")]
        public int? ParentTaskId { get; set; }
    }
}