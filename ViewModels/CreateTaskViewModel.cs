using System.ComponentModel.DataAnnotations;

namespace TaskManagementApp.ViewModels
{
    public class CreateTaskViewModel
    {
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

        [Display(Name = "Piešķirt")]
        [Required(ErrorMessage = "Lūdzu izvēlieties piešķiršanas veidu")]
        public string AssignmentType { get; set; } = "SpecificUser"; // "SpecificUser" vai "AllUsers"

        [Display(Name = "Piešķirtais lietotājs")]
        public string? AssignedUserId { get; set; }

        [Display(Name = "Vecākuzdevums")]
        public int? ParentTaskId { get; set; }
    }
}