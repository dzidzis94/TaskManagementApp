using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagementApp.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nosaukums")]
        [StringLength(200)]
        public string Title { get; set; }

        [Display(Name = "Apraksts")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Display(Name = "Izveides datums")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Termiņš")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Statuss")]
        public TaskStatus Status { get; set; } = TaskStatus.Pending;

        // Rekursīvā saite
        [Display(Name = "Vecākuzdevums")]
        public int? ParentTaskId { get; set; }

        [ForeignKey("ParentTaskId")]
        [Display(Name = "Vecākuzdevums")]
        public virtual TaskItem? ParentTask { get; set; }

        [Display(Name = "Apakšuzdevumi")]
        public virtual ICollection<TaskItem> SubTasks { get; set; } = new List<TaskItem>();

        // Viena lietotāja piešķiršana - MAINĀM UZ NULLABLE
        [Display(Name = "Piešķirtais lietotājs")]
        public string? AssignedUserId { get; set; } // ⬅️ PIEVIENO ?

        [ForeignKey("AssignedUserId")]
        [Display(Name = "Piešķirtais lietotājs")]
        public virtual ApplicationUser? AssignedUser { get; set; } // ⬅️ PIEVIENO ?

        [Display(Name = "Izveidotājs")]
        public string? CreatedById { get; set; } // ⬅️ PIEVIENO ?

        [ForeignKey("CreatedById")]
        [Display(Name = "Izveidotājs")]
        public virtual ApplicationUser? CreatedBy { get; set; } // ⬅️ PIEVIENO ?

        // Vienkāršs veids kā glabāt vairākus izpildītājus (kā komats atdalīta virkne)
        [Display(Name = "Izpildītāji")]
        public string? CompletedByUsers { get; set; } // ⬅️ PIEVIENO ?

        // Vienkāršs veids kā glabāt vairākus piešķirtos lietotājus
        [Display(Name = "Piešķirtie lietotāji")]
        public string? AssignedUserIds { get; set; } // ⬅️ PIEVIENO ?
    }

    public enum TaskStatus
    {
        [Display(Name = "Gaidīšanā")]
        Pending,
        [Display(Name = "Procesā")]
        InProgress,
        [Display(Name = "Pabeigts")]
        Completed,
        [Display(Name = "Atcelts")]
        Cancelled
    }
}