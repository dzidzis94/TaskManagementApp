using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagementApp.Models;

namespace TaskManagementApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Uzdevuma konfigurācija
            builder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(t => t.Id);

                // Rekursīvā saite
                entity.HasOne(t => t.ParentTask)
                    .WithMany(t => t.SubTasks)
                    .HasForeignKey(t => t.ParentTaskId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Saite uz piešķirto lietotāju - PIEVIENO IsRequired(false)
                entity.HasOne(t => t.AssignedUser)
                    .WithMany(u => u.AssignedTasks)
                    .HasForeignKey(t => t.AssignedUserId)
                    .IsRequired(false) // ⬅️ ŠIS IR ĻOTI SVARĪGI!
                    .OnDelete(DeleteBehavior.Restrict);

                // Saite uz izveidotāju - PIEVIENO IsRequired(false)
                entity.HasOne(t => t.CreatedBy)
                    .WithMany(u => u.CreatedTasks)
                    .HasForeignKey(t => t.CreatedById)
                    .IsRequired(false) // ⬅️ ŠIS IR ĻOTI SVARĪGI!
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(t => t.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(t => t.Description)
                    .HasColumnType("nvarchar(max)");

                // PIEVIENO ARĪ ŠO - lai ļautu AssignedUserId būt NULL datu bāzē
                entity.Property(t => t.AssignedUserId)
                    .IsRequired(false);

                entity.Property(t => t.CreatedById)
                    .IsRequired(false);
            });
        }
    }
}