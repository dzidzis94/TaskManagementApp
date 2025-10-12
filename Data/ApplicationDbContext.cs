using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagementApp.Models;

namespace TaskManagementApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TaskAssignment> TaskAssignments { get; set; }
        public DbSet<TaskCompletion> TaskCompletions { get; set; }
        public DbSet<ProjectTemplate> ProjectTemplates { get; set; }
        public DbSet<TemplateSection> TemplateSections { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Project Configuration
            builder.Entity<Project>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.HasMany(p => p.Tasks)
                      .WithOne(t => t.Project)
                      .HasForeignKey(t => t.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade); // If a project is deleted, its tasks are also deleted.
            });

            // ProjectTemplate Configuration
            builder.Entity<ProjectTemplate>(entity =>
            {
                entity.HasKey(pt => pt.Id);
                entity.Property(pt => pt.Name).IsRequired().HasMaxLength(100);
                entity.HasMany(pt => pt.Sections)
                      .WithOne(ts => ts.ProjectTemplate)
                      .HasForeignKey(ts => ts.ProjectTemplateId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TemplateSection Configuration
            builder.Entity<TemplateSection>(entity =>
            {
                entity.HasKey(ts => ts.Id);
                entity.Property(ts => ts.Title).IsRequired().HasMaxLength(100);
                entity.HasOne(ts => ts.ParentSection)
                    .WithMany(ts => ts.ChildSections)
                    .HasForeignKey(ts => ts.ParentSectionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TaskItem Configuration
            builder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(t => t.Id);

                // Recursive relationship for subtasks
                entity.HasOne(t => t.ParentTask)
                    .WithMany(t => t.SubTasks)
                    .HasForeignKey(t => t.ParentTaskId)
                    .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a task with subtasks

                // Relationship with Creator (ApplicationUser)
                entity.HasOne(t => t.CreatedBy)
                    .WithMany(u => u.CreatedTasks) // Explicitly point to the CreatedTasks collection
                    .HasForeignKey(t => t.CreatedById)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull); // Keep task if creator is deleted

                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.Property(t => t.Description).HasColumnType("nvarchar(max)");
            });

            // TaskAssignment (Many-to-Many between Task and User)
            builder.Entity<TaskAssignment>(entity =>
            {
                entity.HasKey(ta => ta.Id);

                // Explicitly define the relationship from TaskAssignment to TaskItem
                entity.HasOne(ta => ta.Task)
                      .WithMany(t => t.TaskAssignments) // In TaskItem, the collection is TaskAssignments
                      .HasForeignKey(ta => ta.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Explicitly define the relationship from TaskAssignment to ApplicationUser
                entity.HasOne(ta => ta.User)
                      .WithMany(u => u.TaskAssignments) // In ApplicationUser, the collection is also TaskAssignments
                      .HasForeignKey(ta => ta.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // TaskCompletion Configuration
            builder.Entity<TaskCompletion>(entity =>
            {
                entity.HasKey(tc => tc.Id);
                entity.HasOne(tc => tc.Task)
                      .WithMany(t => t.TaskCompletions)
                      .HasForeignKey(tc => tc.TaskId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tc => tc.User)
                      .WithMany() // A user can complete many tasks
                      .HasForeignKey(tc => tc.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}