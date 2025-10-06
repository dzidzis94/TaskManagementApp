using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TaskManagementApp.Data;
using TaskManagementApp.Models;
using TaskManagementApp.ViewModels;

namespace TaskManagementApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // 1. Get Recent Activities
            var createdTasks = await _context.Tasks
                .Where(t => t.CreatedAt >= thirtyDaysAgo)
                .Include(t => t.CreatedBy)
                .Select(t => new ActivityItem {
                    Message = $"created task '{t.Title}'",
                    Timestamp = t.CreatedAt,
                    UserFullName = t.CreatedBy.UserName,
                    ActivityType = "New Task",
                    RelatedId = t.Id
                })
                .ToListAsync();

            var completedTasksActivity = await _context.TaskCompletions
                .Where(tc => tc.CompletionDate >= thirtyDaysAgo)
                .Include(tc => tc.User)
                .Include(tc => tc.Task)
                .Select(tc => new ActivityItem {
                    Message = $"completed task '{tc.Task.Title}'",
                    Timestamp = tc.CompletionDate,
                    UserFullName = tc.User.UserName,
                    ActivityType = "Task Completed",
                    RelatedId = tc.TaskId
                })
                .ToListAsync();

            var recentActivity = createdTasks
                .Concat(completedTasksActivity)
                .OrderByDescending(a => a.Timestamp)
                .Take(20) // Limit to the last 20 activities
                .ToList();

            // 2. Get Task Statistics for the current user
            var myAssignedTaskIds = await _context.TaskAssignments
                .Where(ta => ta.UserId == currentUserId)
                .Select(ta => ta.TaskId)
                .ToListAsync();

            var myCompletedTaskIds = await _context.TaskCompletions
                .Where(tc => tc.UserId == currentUserId && myAssignedTaskIds.Contains(tc.TaskId))
                .Select(tc => tc.TaskId)
                .ToListAsync();

            var totalTasks = myAssignedTaskIds.Count;
            var completedCount = myCompletedTaskIds.Count;
            var completionPercentage = (totalTasks > 0) ? ((double)completedCount / totalTasks) * 100 : 0;

            // 3. Get outstanding tasks for the current user
            var myOutstandingTasks = await _context.Tasks
                .Where(t => myAssignedTaskIds.Contains(t.Id) && !myCompletedTaskIds.Contains(t.Id))
                .OrderBy(t => t.DueDate)
                .Take(10) // Limit to 10 pressing tasks
                .ToListAsync();


            var model = new DashboardViewModel
            {
                RecentActivity = recentActivity,
                TotalTasks = totalTasks,
                CompletedTasks = completedCount,
                CompletionPercentage = completionPercentage,
                MyTasks = myOutstandingTasks
            };

            return View(model);
        }
    }
}