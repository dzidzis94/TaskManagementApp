using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagementApp.Data;
using TaskManagementApp.Models;
using TaskManagementApp.ViewModels;
using System.Security.Claims;

namespace TaskManagementApp.Controllers
{
    [Authorize]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TasksController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Tasks
        public async Task<IActionResult> Index()
        {
            // Ielādējam tikai saknes uzdevumus (bez vecākiem) ar LEFT JOIN
            var rootTasks = await _context.Tasks
                .Include(t => t.AssignedUser)  // LEFT JOIN automātiski
                .Include(t => t.CreatedBy)     // LEFT JOIN automātiski
                .Where(t => t.ParentTaskId == null)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Manuāli ielādējam VISUS apakšuzdevumus rekursīvi
            foreach (var task in rootTasks)
            {
                await LoadAllSubTasksRecursively(task);
            }

            return View(rootTasks);
        }

        // Palīgmetode rekursīvai VISU apakšuzdevumu ielādei
        private async Task LoadAllSubTasksRecursively(TaskItem task)
        {
            // Ielādējam visus apakšuzdevumus ar LEFT JOIN
            var subTasks = await _context.Tasks
                .Include(t => t.AssignedUser)  // LEFT JOIN
                .Include(t => t.CreatedBy)     // LEFT JOIN
                .Where(t => t.ParentTaskId == task.Id)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            task.SubTasks = subTasks;

            // Rekursīvi ielādējam VISUS apakšuzdevumus
            foreach (var subTask in subTasks)
            {
                await LoadAllSubTasksRecursively(subTask);
            }
        }

        // GET: Tasks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.AssignedUser)  // LEFT JOIN
                .Include(t => t.CreatedBy)     // LEFT JOIN
                .Include(t => t.ParentTask)    // LEFT JOIN
                .FirstOrDefaultAsync(m => m.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            // Ielādējam VISUS apakšuzdevumus rekursīvi
            await LoadAllSubTasksRecursively(task);

            return View(task);
        }

        // GET: Tasks/Create - ŠIS TRUKST! PIEVIENO ŠO:
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(int? parentTaskId = null)
        {
            var users = await _userManager.Users.ToListAsync();

            var model = new CreateTaskViewModel
            {
                ParentTaskId = parentTaskId,
                DueDate = DateTime.Today.AddDays(7)
            };

            ViewBag.Users = users;

            // Pārbaudām vai vecākuzdevums eksistē
            if (parentTaskId.HasValue)
            {
                var parentTask = await _context.Tasks
                    .FirstOrDefaultAsync(t => t.Id == parentTaskId.Value);

                if (parentTask != null)
                {
                    ViewBag.ParentTaskTitle = parentTask.Title;
                }
            }

            return View(model);
        }

        // POST: Tasks/Create - TURI TIKAI ŠO VIENU POST METODI
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Users = await _userManager.Users.ToListAsync();
                return View(model);
            }

            // Pārbaudām vai vecākuzdevums eksistē
            if (model.ParentTaskId.HasValue)
            {
                var parentTask = await _context.Tasks.FindAsync(model.ParentTaskId.Value);
                if (parentTask == null)
                {
                    ModelState.AddModelError("ParentTaskId", "Norādītais vecākuzdevums neeksistē");
                    ViewBag.Users = await _userManager.Users.ToListAsync();
                    return View(model);
                }
            }

            // Validācija piešķiršanas tipam
            if (model.AssignmentType == "SpecificUser" && string.IsNullOrEmpty(model.AssignedUserId))
            {
                ModelState.AddModelError("AssignedUserId", "Lūdzu izvēlieties lietotāju");
                ViewBag.Users = await _userManager.Users.ToListAsync();
                return View(model);
            }

            // Ja piešķir visiem lietotājiem, tad AssignedUserId būs null
            string assignedUserId = model.AssignmentType == "AllUsers" ? null : model.AssignedUserId;

            var task = new TaskItem
            {
                Title = model.Title,
                Description = model.Description,
                DueDate = model.DueDate,
                ParentTaskId = model.ParentTaskId,
                AssignedUserId = assignedUserId, // Var būt null
                CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.UtcNow,
                Status = Models.TaskStatus.Pending,
                // Inicializējam string īpašības
                CompletedByUsers = "",
                AssignedUserIds = ""
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Uzdevums veiksmīgi izveidots!";

            return model.ParentTaskId.HasValue
                ? RedirectToAction("Details", new { id = model.ParentTaskId.Value })
                : RedirectToAction(nameof(Index));
        }

        // GET: Tasks/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.AssignedUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            var users = await _userManager.Users.ToListAsync();
            ViewBag.Users = users;

            var model = new EditTaskViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Status = task.Status,
                AssignedUserId = task.AssignedUserId,
                ParentTaskId = task.ParentTaskId
            };

            return View(model);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, EditTaskViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                {
                    return NotFound();
                }

                task.Title = model.Title;
                task.Description = model.Description;
                task.DueDate = model.DueDate;
                task.Status = model.Status;
                task.AssignedUserId = model.AssignedUserId;

                try
                {
                    _context.Update(task);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Uzdevums veiksmīgi atjaunināts!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var users = await _userManager.Users.ToListAsync();
            ViewBag.Users = users;
            return View(model);
        }

        // POST: Tasks/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.SubTasks)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            // Pārbauda vai uzdevumam nav apakšuzdevumu
            if (task.SubTasks.Any())
            {
                TempData["ErrorMessage"] = "Nevar dzēst uzdevumu, kam ir apakšuzdevumi!";
                return RedirectToAction(nameof(Index));
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Uzdevums veiksmīgi dzēsts!";

            return RedirectToAction(nameof(Index));
        }

        // POST: Tasks/ChangeStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(int id, Models.TaskStatus newStatus)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            // Pārbauda vai lietotājs var mainīt statusu
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (task.AssignedUserId != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Jums nav tiesību mainīt šī uzdevuma statusu!";
                return RedirectToAction(nameof(Index));
            }

            task.Status = newStatus;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Uzdevuma statuss veiksmīgi mainīts!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Tasks/JoinTask/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Pārbauda vai lietotājs jau nav piešķirts
            if (!IsUserInAssignedList(task.AssignedUserIds, currentUserId))
            {
                // Pievieno lietotāju assignedUserIds sarakstam
                task.AssignedUserIds = AddUserToList(task.AssignedUserIds, currentUserId);

                _context.Update(task);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Jūs veiksmīgi pievienojāties uzdevumam!";
            }
            else
            {
                TempData["InfoMessage"] = "Jūs jau esat piešķirts šim uzdevumam!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Tasks/MarkAsCompleted/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCompleted(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Pārbauda vai lietotājs jau nav atzīmējis kā pabeigtu
            if (!IsUserInCompletedList(task.CompletedByUsers, currentUserId))
            {
                // Pievieno lietotāju completedByUsers sarakstam
                task.CompletedByUsers = AddUserToList(task.CompletedByUsers, currentUserId);
                task.Status = Models.TaskStatus.Completed;

                _context.Update(task);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Uzdevums atzīmēts kā pabeigts!";
            }
            else
            {
                TempData["InfoMessage"] = "Jūs jau esat atzīmējis šo uzdevumu kā pabeigtu!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }

        // Palīgmetodes
        private bool IsUserInCompletedList(string completedByUsers, string userId)
        {
            if (string.IsNullOrEmpty(completedByUsers))
                return false;

            return completedByUsers.Split(',').Contains(userId);
        }

        private bool IsUserInAssignedList(string assignedUserIds, string userId)
        {
            if (string.IsNullOrEmpty(assignedUserIds))
                return false;

            return assignedUserIds.Split(',').Contains(userId);
        }

        private string AddUserToList(string userList, string userId)
        {
            if (string.IsNullOrEmpty(userList))
                return userId;

            var users = userList.Split(',').ToList();
            if (!users.Contains(userId))
            {
                users.Add(userId);
            }

            return string.Join(",", users);
        }
    }
}