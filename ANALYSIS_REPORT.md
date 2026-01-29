# System Architecture Review: Project & Task Cloning

## 1. TaskItem and Project Relationship

The relationship between `Project` and `TaskItem` follows a standard **One-to-Many** pattern, while `TaskItem` itself implements a **Self-Referencing** hierarchy (Adjacency List pattern).

*   **One-to-Many (Project ↔ Task):**
    *   A `Project` contains a collection of tasks: `public virtual ICollection<TaskItem> Tasks { get; set; }`
    *   A `TaskItem` belongs to a project: `public int? ProjectId { get; set; }` and `public virtual Project Project { get; set; }`

*   **Self-Referencing (Task ↔ SubTasks):**
    *   A `TaskItem` can have a parent: `public int? ParentTaskId { get; set; }`
    *   A `TaskItem` can have multiple sub-tasks: `public virtual ICollection<TaskItem> SubTasks { get; set; }`

## 2. Current `TaskService.CloneTaskAsync` Implementation

The `TaskService` handles task cloning by recursively creating and saving entities *one by one* inside the database transaction.

**File:** `Services/TaskService.cs`

```csharp
public async Task<int> CloneTaskAsync(int sourceTaskId, int? targetProjectId, string userId, int? newParentId = null)
{
    var sourceTaskRoot = await GetTaskTreeAsNoTrackingAsync(sourceTaskId);
    if (sourceTaskRoot == null) throw new ArgumentException("Task not found");

    using (var transaction = await _context.Database.BeginTransactionAsync())
    {
        try
        {
            var newTaskId = await CloneTaskRecursiveAsync(sourceTaskRoot, targetProjectId, userId, newParentId);
            await transaction.CommitAsync();
            return newTaskId;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

private async Task<int> CloneTaskRecursiveAsync(TaskItem sourceTask, int? targetProjectId, string userId, int? newParentId)
{
    // Create a new object to ensure no tracking conflicts and Id is 0
    var newTask = new TaskItem
    {
        Title = sourceTask.Title,
        Description = sourceTask.Description,
        Priority = sourceTask.Priority,
        ProjectId = targetProjectId ?? sourceTask.ProjectId,
        ParentTaskId = newParentId,
        CreatedById = userId,
        CreatedAt = DateTime.UtcNow,
        Status = Models.TaskStatus.Pending,
        TaskAssignments = new List<TaskAssignment>(),
        TaskCompletions = new List<TaskCompletion>()
    };

    _context.Tasks.Add(newTask);
    await _context.SaveChangesAsync(); // N+1 Insert Pattern

    if (sourceTask.SubTasks != null)
    {
        foreach (var subTask in sourceTask.SubTasks)
        {
            await CloneTaskRecursiveAsync(subTask, targetProjectId, userId, newTask.Id);
        }
    }

    return newTask.Id;
}
```

## 3. "Clone Project" Integration

**Finding:** The "Clone Project" feature (`TemplateService.CloneProjectAsync`) does **NOT** call `TaskService.CloneTaskAsync`.

Instead, it implements its own completely separate recursive logic to clone tasks.

*   **Location:** `Services/TemplateService.cs`
*   **Method:** `CloneProjectAsync` calls a private helper `CreateTaskFromTaskRecursive`.
*   **Difference:** Unlike `TaskService`, the `TemplateService` builds the entire hierarchy of new `TaskItem` objects in memory first, adds them to a list, and then saves them in a single batch operation (`_context.Tasks.AddRange(tasksToAdd)`), which is generally more performant but represents a code duplication issue.

```csharp
// Inside Services/TemplateService.cs

public async Task<Project> CloneProjectAsync(...) {
    // ... (Project creation logic)

    // It manually rebuilds the hierarchy
    foreach (var task in rootTasks)
    {
        var newTask = CreateTaskFromTaskRecursive(task, targetProject, null, excludedTaskIdsSet);
        if (newTask != null)
        {
            tasksToAdd.Add(newTask);
        }
    }

    _context.Tasks.AddRange(tasksToAdd); // Batch Save
    await _context.SaveChangesAsync();
}
```

**Conclusion for Refactoring:**
There are currently two independent implementations of task cloning logic (one in `TaskService`, one in `TemplateService`). A Serialization-based refactor should aim to unify these into a single service method that handles both deep-cloning a single task and deep-cloning a list of tasks for a project.
