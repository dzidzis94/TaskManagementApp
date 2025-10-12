using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TaskManagementApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [Display(Name = "Vārds")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Uzvārds")]
        public string LastName { get; set; }

        [Display(Name = "Izveidots")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
        public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
    }
}