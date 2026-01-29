using System.Collections.Generic;
using System.Linq;
using TaskManagementApp.Models;
using TaskManagementApp.ViewModels;

namespace TaskManagementApp.Helpers
{
    public static class HierarchyHelper
    {
        /// <summary>
        /// Pārvērš TaskItem sarakstu par hierarhisku TaskSummaryViewModel koku.
        /// </summary>
        public static List<TaskSummaryViewModel> BuildTaskHierarchy(ICollection<TaskItem> allTasks)
        {
            if (allTasks == null || !allTasks.Any())
            {
                return new List<TaskSummaryViewModel>();
            }

            // Pārvēršam par List, lai vieglāk strādāt
            var taskList = allTasks.ToList();

            // Atrodam tikai galvenos uzdevumus (kur ParentTaskId ir null)
            var rootTasks = taskList.Where(t => t.ParentTaskId == null).ToList();
            var result = new List<TaskSummaryViewModel>();

            foreach (var task in rootTasks)
            {
                result.Add(MapToViewModelRecursive(task, taskList));
            }

            return result;
        }

        /// <summary>
        /// Rekursīva palīgmetode, kas kartē uzdevumu un meklē tā bērnus.
        /// </summary>
        private static TaskSummaryViewModel MapToViewModelRecursive(TaskItem task, List<TaskItem> allTasks)
        {
            var viewModel = new TaskSummaryViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                ProjectId = task.ProjectId,
                ParentTaskId = task.ParentTaskId,
                CreatedAt = task.CreatedAt,
                // Ja vajag, inicializējam tukšus sarakstus, lai nebūtu null warningi
                AssignedUserNames = new List<string>(),
                SubTasks = new List<TaskSummaryViewModel>()
            };

            // Atrodam bērnus (kur ParentTaskId sakrīt ar šī uzdevuma ID)
            var children = allTasks
                .Where(t => t.ParentTaskId == task.Id)
                .OrderBy(t => t.CreatedAt) // Kārtojam pēc vecuma
                .ToList();

            if (children.Any())
            {
                foreach (var child in children)
                {
                    // Rekursīvi izsaucam sevi pašu priekš bērna
                    viewModel.SubTasks.Add(MapToViewModelRecursive(child, allTasks));
                }
            }

            return viewModel;
        }
    }
}