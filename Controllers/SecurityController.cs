using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartExpenseTracker.Data;
using SmartExpenseTracker.Models;
using SmartExpenseTracker.ViewModels;

namespace SmartExpenseTracker.Controllers
{
    [Authorize]
    public class SecurityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SecurityController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            // Get user statistics
            var totalExpenses = await _context.Expenses.CountAsync(e => e.UserId == userId);
            var totalIncome = await _context.Incomes.CountAsync(i => i.UserId == userId);
            var totalBudgets = await _context.Budgets.CountAsync(b => b.UserId == userId);

            // Get account creation date
            var accountCreated = user?.LockoutEnd?.DateTime ?? DateTime.Now; // Fallback if not available

            var viewModel = new SecurityViewModel
            {
                UserEmail = user?.Email ?? "Unknown",
                AccountCreatedDate = accountCreated,
                TotalExpenses = totalExpenses,
                TotalIncome = totalIncome,
                TotalBudgets = totalBudgets,
                LastLoginDate = DateTime.Now, // This would typically come from a tracking system
                TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
                EmailConfirmed = user?.EmailConfirmed ?? false
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult ExportData()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ExportData(string format)
        {
            var userId = _userManager.GetUserId(User);

            // Get all user data
            var expenses = await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            var income = await _context.Incomes
                .Include(i => i.Category)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            var budgets = await _context.Budgets
                .Include(b => b.Category)
                .Where(b => b.UserId == userId)
                .ToListAsync();

            if (format?.ToLower() == "csv")
            {
                return await ExportToCsv(expenses, income, budgets);
            }

            // Default to JSON export
            return await ExportToJson(expenses, income, budgets);
        }

        [HttpGet]
        public IActionResult DeleteAccount()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccountConfirmed()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user != null)
            {
                // Delete all user data
                var expenses = await _context.Expenses.Where(e => e.UserId == userId).ToListAsync();
                var income = await _context.Incomes.Where(i => i.UserId == userId).ToListAsync();
                var budgets = await _context.Budgets.Where(b => b.UserId == userId).ToListAsync();

                _context.Expenses.RemoveRange(expenses);
                _context.Incomes.RemoveRange(income);
                _context.Budgets.RemoveRange(budgets);

                await _context.SaveChangesAsync();

                // Delete user account
                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Your account and all associated data have been permanently deleted.";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["ErrorMessage"] = "An error occurred while deleting your account. Please try again.";
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAllData()
        {
            var userId = _userManager.GetUserId(User);

            // Delete all user financial data but keep the account
            var expenses = await _context.Expenses.Where(e => e.UserId == userId).ToListAsync();
            var income = await _context.Incomes.Where(i => i.UserId == userId).ToListAsync();
            var budgets = await _context.Budgets.Where(b => b.UserId == userId).ToListAsync();

            _context.Expenses.RemoveRange(expenses);
            _context.Incomes.RemoveRange(income);
            _context.Budgets.RemoveRange(budgets);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All your financial data has been cleared successfully.";
            return RedirectToAction("Index");
        }

        private async Task<IActionResult> ExportToCsv(List<Expense> expenses, List<Income> income, List<Budget> budgets)
        {
            var csv = new System.Text.StringBuilder();
            
            // Add header
            csv.AppendLine("Data Export - Smart Expense Tracker");
            csv.AppendLine($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();

            // Expenses section
            csv.AppendLine("EXPENSES");
            csv.AppendLine("Date,Title,Category,Amount,Description,Created Date,Last Modified");
            
            foreach (var expense in expenses)
            {
                csv.AppendLine($"{expense.Date:yyyy-MM-dd},{expense.Title},{expense.Category.Name},{expense.Amount},{expense.Description},{expense.CreatedDate:yyyy-MM-dd HH:mm:ss},{expense.ModifiedDate:yyyy-MM-dd HH:mm:ss}");
            }

            csv.AppendLine();

            // Income section
            csv.AppendLine("INCOME");
            csv.AppendLine("Date,Title,Category,Amount,Description,Created Date,Last Modified");
            
            foreach (var inc in income)
            {
                csv.AppendLine($"{inc.Date:yyyy-MM-dd},{inc.Title},{inc.Category.Name},{inc.Amount},{inc.Description},{inc.CreatedDate:yyyy-MM-dd HH:mm:ss},{inc.ModifiedDate:yyyy-MM-dd HH:mm:ss}");
            }

            csv.AppendLine();

            // Budgets section
            csv.AppendLine("BUDGETS");
            csv.AppendLine("Name,Category,Amount,Start Date,End Date,Is Active,Description,Created Date,Last Modified");
            
            foreach (var budget in budgets)
            {
                csv.AppendLine($"{budget.Name},{budget.Category.Name},{budget.Amount},{budget.StartDate:yyyy-MM-dd},{budget.EndDate:yyyy-MM-dd},{budget.IsActive},{budget.Description},{budget.CreatedDate:yyyy-MM-dd HH:mm:ss},{budget.ModifiedDate:yyyy-MM-dd HH:mm:ss}");
            }

            var fileName = $"SmartExpenseTracker_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        private async Task<IActionResult> ExportToJson(List<Expense> expenses, List<Income> income, List<Budget> budgets)
        {
            var exportData = new
            {
                ExportDate = DateTime.Now,
                Expenses = expenses.Select(e => new
                {
                    e.Date,
                    e.Title,
                    Category = e.Category.Name,
                    e.Amount,
                    e.Description,
                    e.CreatedDate,
                    e.ModifiedDate
                }),
                Income = income.Select(i => new
                {
                    i.Date,
                    i.Title,
                    Category = i.Category.Name,
                    i.Amount,
                    i.Description,
                    i.CreatedDate,
                    i.ModifiedDate
                }),
                Budgets = budgets.Select(b => new
                {
                    b.Name,
                    Category = b.Category.Name,
                    b.Amount,
                    b.StartDate,
                    b.EndDate,
                    b.IsActive,
                    b.Description,
                    b.CreatedDate,
                    b.ModifiedDate
                })
            };

            var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var fileName = $"SmartExpenseTracker_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
        }
    }
}