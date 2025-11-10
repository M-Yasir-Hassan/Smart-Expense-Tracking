using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartExpenseTracker.Data;
using SmartExpenseTracker.Models;
using SmartExpenseTracker.ViewModels;

namespace SmartExpenseTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new AdminDashboardViewModel();

            // Get user statistics
            var allUsers = await _userManager.Users.ToListAsync();
            viewModel.TotalUsers = allUsers.Count;
            viewModel.PendingApprovals = allUsers.Count(u => !u.IsApproved);
            viewModel.ApprovedUsers = allUsers.Count(u => u.IsApproved);
            viewModel.ActiveUsers = allUsers.Count(u => u.IsActive);

            // Get financial statistics
            viewModel.TotalExpenses = await _context.Expenses.CountAsync();
            viewModel.TotalIncome = await _context.Incomes.CountAsync();
            viewModel.TotalBudgets = await _context.Budgets.CountAsync();
            viewModel.TotalExpenseAmount = await _context.Expenses.SumAsync(e => (decimal?)e.Amount) ?? 0;
            viewModel.TotalIncomeAmount = await _context.Incomes.SumAsync(i => (decimal?)i.Amount) ?? 0;

            // Get recent registrations
            viewModel.RecentRegistrations = allUsers
                .OrderByDescending(u => u.RegistrationDate)
                .Take(5)
                .Select(u => new RecentUserRegistration
                {
                    UserId = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? "",
                    RegistrationDate = u.RegistrationDate,
                    IsApproved = u.IsApproved
                })
                .ToList();

            // Get user activities
            var userActivities = new List<UserActivitySummary>();
            foreach (var user in allUsers.Take(10))
            {
                var expenseCount = await _context.Expenses.CountAsync(e => e.UserId == user.Id);
                var incomeCount = await _context.Incomes.CountAsync(i => i.UserId == user.Id);
                var budgetCount = await _context.Budgets.CountAsync(b => b.UserId == user.Id);
                var totalExpenses = await _context.Expenses.Where(e => e.UserId == user.Id).SumAsync(e => (decimal?)e.Amount) ?? 0;
                var totalIncome = await _context.Incomes.Where(i => i.UserId == user.Id).SumAsync(i => (decimal?)i.Amount) ?? 0;

                userActivities.Add(new UserActivitySummary
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    LastLoginDate = user.LastLoginDate,
                    ExpenseCount = expenseCount,
                    IncomeCount = incomeCount,
                    BudgetCount = budgetCount,
                    TotalExpenses = totalExpenses,
                    TotalIncome = totalIncome
                });
            }
            viewModel.UserActivities = userActivities;

            // Get monthly registration data for charts
            var monthlyRegsList = allUsers
                .GroupBy(u => new { u.RegistrationDate.Year, u.RegistrationDate.Month })
                .Select(g => new { Date = new DateTime(g.Key.Year, g.Key.Month, 1), Count = g.Count() })
                .OrderBy(x => x.Date)
                .Take(12)
                .ToList();
            
            var monthlyRegs = monthlyRegsList.Any() 
                ? monthlyRegsList.ToDictionary(x => x.Date.ToString("MMM yyyy"), x => x.Count)
                : new Dictionary<string, int>();
            viewModel.MonthlyRegistrations = monthlyRegs;

            // Get monthly expense data - simplified to avoid LINQ issues
            try
            {
                var hasExpenses = await _context.Expenses.AnyAsync();
                if (hasExpenses)
                {
                    var expenseData = await _context.Expenses
                        .Select(e => new { e.Date, e.Amount })
                        .ToListAsync();
                    
                    var monthlyExpenses = expenseData
                        .GroupBy(e => new { e.Date.Year, e.Date.Month })
                        .Select(g => new { Date = new DateTime(g.Key.Year, g.Key.Month, 1), Amount = g.Sum(e => e.Amount) })
                        .OrderBy(x => x.Date)
                        .Take(12)
                        .ToDictionary(x => x.Date.ToString("MMM yyyy"), x => x.Amount);
                    
                    viewModel.MonthlyExpenses = monthlyExpenses;
                }
                else
                {
                    viewModel.MonthlyExpenses = new Dictionary<string, decimal>();
                }
            }
            catch (Exception)
            {
                // Fallback to empty dictionary if there's any issue
                viewModel.MonthlyExpenses = new Dictionary<string, decimal>();
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Users(string searchTerm = "", string statusFilter = "All", int page = 1)
        {
            var pageSize = 10;
            var query = _userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.FirstName.Contains(searchTerm) || 
                                        u.LastName.Contains(searchTerm) || 
                                        u.Email!.Contains(searchTerm));
            }

            // Apply status filter
            switch (statusFilter)
            {
                case "Pending":
                    query = query.Where(u => !u.IsApproved);
                    break;
                case "Approved":
                    query = query.Where(u => u.IsApproved);
                    break;
                case "Inactive":
                    query = query.Where(u => !u.IsActive);
                    break;
            }

            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var users = await query
                .OrderByDescending(u => u.RegistrationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userListItems = new List<UserListItem>();
            foreach (var user in users)
            {
                var expenseCount = await _context.Expenses.CountAsync(e => e.UserId == user.Id);
                var incomeCount = await _context.Incomes.CountAsync(i => i.UserId == user.Id);
                var budgetCount = await _context.Budgets.CountAsync(b => b.UserId == user.Id);

                userListItems.Add(new UserListItem
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? "",
                    IsApproved = user.IsApproved,
                    IsActive = user.IsActive,
                    RegistrationDate = user.RegistrationDate,
                    ApprovedDate = user.ApprovedDate,
                    LastLoginDate = user.LastLoginDate,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    ExpenseCount = expenseCount,
                    IncomeCount = incomeCount,
                    BudgetCount = budgetCount
                });
            }

            var viewModel = new UserManagementViewModel
            {
                Users = userListItems,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(viewModel);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Get user statistics
            var statistics = new UserStatistics
            {
                TotalExpenses = await _context.Expenses.CountAsync(e => e.UserId == id),
                TotalIncome = await _context.Incomes.CountAsync(i => i.UserId == id),
                TotalBudgets = await _context.Budgets.CountAsync(b => b.UserId == id),
                TotalExpenseAmount = await _context.Expenses.Where(e => e.UserId == id).SumAsync(e => (decimal?)e.Amount) ?? 0,
                TotalIncomeAmount = await _context.Incomes.Where(i => i.UserId == id).SumAsync(i => (decimal?)i.Amount) ?? 0,
                CurrentMonthExpenses = await _context.Expenses
                    .Where(e => e.UserId == id && e.Date.Month == DateTime.Now.Month && e.Date.Year == DateTime.Now.Year)
                    .SumAsync(e => (decimal?)e.Amount) ?? 0,
                CurrentMonthIncome = await _context.Incomes
                    .Where(i => i.UserId == id && i.Date.Month == DateTime.Now.Month && i.Date.Year == DateTime.Now.Year)
                    .SumAsync(i => (decimal?)i.Amount) ?? 0
            };

            // Get recent activities
            var recentActivities = new List<RecentActivity>();
            
            var recentExpenses = await _context.Expenses
                .Where(e => e.UserId == id)
                .OrderByDescending(e => e.Date)
                .Take(5)
                .ToListAsync();
            
            recentActivities.AddRange(recentExpenses.Select(e => new RecentActivity
            {
                Type = "Expense",
                Description = e.Title,
                Amount = e.Amount,
                Date = e.Date
            }));

            var recentIncomes = await _context.Incomes
                .Where(i => i.UserId == id)
                .OrderByDescending(i => i.Date)
                .Take(5)
                .ToListAsync();
            
            recentActivities.AddRange(recentIncomes.Select(i => new RecentActivity
            {
                Type = "Income",
                Description = i.Title,
                Amount = i.Amount,
                Date = i.Date
            }));

            recentActivities = recentActivities.OrderByDescending(a => a.Date).Take(10).ToList();

            var viewModel = new UserDetailsViewModel
            {
                User = user,
                Roles = roles.ToList(),
                Statistics = statistics,
                RecentActivities = recentActivities
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser([FromBody] string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var currentUserId = _userManager.GetUserId(User);
            user.IsApproved = true;
            user.ApprovedDate = DateTime.UtcNow;
            user.ApprovedByUserId = currentUserId;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Add user to User role if not already assigned
                if (!await _userManager.IsInRoleAsync(user, "User"))
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                return Json(new { success = true, message = "User approved successfully" });
            }

            return Json(new { success = false, message = "Failed to approve user" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser([FromBody] string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            user.IsApproved = false;
            user.ApprovedDate = null;
            user.ApprovedByUserId = null;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "User approval revoked" });
            }

            return Json(new { success = false, message = "Failed to revoke user approval" });
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                IsApproved = user.IsApproved,
                IsActive = user.IsActive,
                Notes = user.Notes,
                SelectedRoles = userRoles.ToList(),
                AvailableRoles = allRoles!
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Update user properties
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.IsApproved = model.IsApproved;
            user.IsActive = model.IsActive;
            user.Notes = model.Notes;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                var rolesToRemove = currentRoles.Except(model.SelectedRoles);
                var rolesToAdd = model.SelectedRoles.Except(currentRoles);

                await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                await _userManager.AddToRolesAsync(user, rolesToAdd);

                TempData["SuccessMessage"] = "User updated successfully";
                return RedirectToAction("UserDetails", new { id = model.Id });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser([FromBody] string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            // Don't allow deleting admin users
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Json(new { success = false, message = "Cannot delete admin users" });
            }

            // Delete user's financial data
            var expenses = await _context.Expenses.Where(e => e.UserId == id).ToListAsync();
            var incomes = await _context.Incomes.Where(i => i.UserId == id).ToListAsync();
            var budgets = await _context.Budgets.Where(b => b.UserId == id).ToListAsync();
            var notifications = await _context.Notifications.Where(n => n.UserId == id).ToListAsync();
            var notificationPrefs = await _context.NotificationPreferences.Where(np => np.UserId == id).ToListAsync();

            _context.Expenses.RemoveRange(expenses);
            _context.Incomes.RemoveRange(incomes);
            _context.Budgets.RemoveRange(budgets);
            _context.Notifications.RemoveRange(notifications);
            _context.NotificationPreferences.RemoveRange(notificationPrefs);

            await _context.SaveChangesAsync();

            // Delete the user
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "User deleted successfully" });
            }

            return Json(new { success = false, message = "Failed to delete user" });
        }
    }
}
