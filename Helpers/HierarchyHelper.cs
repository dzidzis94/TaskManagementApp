using System.Collections.Generic;
using System.Linq;
using TaskManagementApp.Models;

namespace TaskManagementApp.Helpers
{
    public static class HierarchyHelper
    {
        public static List<TaskItem> BuildTaskHierarchy(IEnumerable<TaskItem> allTasks)
        {
            var taskDict = allTasks.ToDictionary(t => t.Id);
            var rootTasks = new List<TaskItem>();

            foreach (var task in allTasks)
            {
                // Ensure SubTasks list is initialized
                if (task.SubTasks == null)
                {
                    task.SubTasks = new List<TaskItem>();
                }
                else
                {
                    // Clear any existing sub-tasks to prevent duplication if the collection is pre-populated
                    task.SubTasks.Clear();
                }
            }

            foreach (var task in allTasks)
            {
                if (task.ParentTaskId.HasValue && taskDict.TryGetValue(task.ParentTaskId.Value, out var parent))
                {
                    parent.SubTasks.Add(task);
                }
                else
                {
                    rootTasks.Add(task);
                }
            }

            return rootTasks;
        }
    }
}
