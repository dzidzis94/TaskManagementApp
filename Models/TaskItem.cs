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

        [Display(Name = "Projekts")]
        public int? ProjectId { get; set; }

        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        [Display(Name = "Izveidotājs")]
        public string? CreatedById { get; set; }

        [ForeignKey("CreatedById")]
        public virtual ApplicationUser? CreatedBy { get; set; }

        public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
        public virtual ICollection<TaskCompletion> TaskCompletions { get; set; } = new List<TaskCompletion>();
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