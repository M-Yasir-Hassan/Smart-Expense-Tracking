using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartExpenseTracker.Data;
using SmartExpenseTracker.Models;
using SmartExpenseTracker.Services;

namespace SmartExpenseTracker.Controllers
{
    [Authorize]
    public class ExpenseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly INotificationService _notificationService;

        public ExpenseController(ApplicationDbContext context, UserManager<IdentityUser> userManager, INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: Expense
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var expenses = await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            return View(expenses);
        }

        // GET: Expense/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var expense = await _context.Expenses
                .Include(e => e.Category)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (expense == null)
            {
                return NotFound();
            }

            return View(expense);
        }

        // GET: Expense/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Expense/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Amount,Date,CategoryId")] Expense expense)
        {
            // Debug: Log ModelState validation errors
            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== EXPENSE MODELSTATE VALIDATION ERRORS ===");
                foreach (var modelError in ModelState)
                {
                    foreach (var error in modelError.Value.Errors)
                    {
                        Console.WriteLine($"ModelState Error - Key: {modelError.Key}, Error: {error.ErrorMessage}");
                    }
                }
                Console.WriteLine("=== END EXPENSE VALIDATION ERRORS ===");
            }

            if (ModelState.IsValid)
            {
                expense.UserId = _userManager.GetUserId(User)!;
                expense.CreatedDate = DateTime.Now;
                _context.Add(expense);
                await _context.SaveChangesAsync();

                // Check for budget notifications after adding expense
                await CheckBudgetNotifications(expense.UserId, expense.CategoryId, expense.Amount);

                // Create expense notification if enabled
                await _notificationService.CreateNewExpenseNotificationAsync(expense.UserId, expense.Title, expense.Amount);

                TempData["Success"] = "Expense created successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", expense.CategoryId);
            return View(expense);
        }

        // GET: Expense/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
            if (expense == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", expense.CategoryId);
            return View(expense);
        }

        // POST: Expense/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Amount,Date,CategoryId,UserId,CreatedDate")] Expense expense)
        {
            if (id != expense.Id)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (expense.UserId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    expense.ModifiedDate = DateTime.Now;
                    _context.Update(expense);
                    await _context.SaveChangesAsync();

                    // Check for budget notifications after updating expense
                    await CheckBudgetNotifications(expense.UserId, expense.CategoryId, expense.Amount);

                    TempData["Success"] = "Expense updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExpenseExists(expense.Id))
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
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", expense.CategoryId);
            return View(expense);
        }

        // GET: Expense/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var expense = await _context.Expenses
                .Include(e => e.Category)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (expense == null)
            {
                return NotFound();
            }

            return View(expense);
        }

        // POST: Expense/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Expense deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ExpenseExists(int id)
        {
            return _context.Expenses.Any(e => e.Id == id);
        }

        private async Task CheckBudgetNotifications(string userId, int categoryId, decimal expenseAmount)
        {
            // Get active budgets for this category
            var activeBudgets = await _context.Budgets
                .Where(b => b.UserId == userId && 
                           b.CategoryId == categoryId && 
                           b.IsActive && 
                           DateTime.Now >= b.StartDate && 
                           DateTime.Now <= b.EndDate)
                .ToListAsync();

            foreach (var budget in activeBudgets)
            {
                // Calculate total spent for this budget period
                var totalSpent = await _context.Expenses
                    .Where(e => e.UserId == userId && 
                               e.CategoryId == budget.CategoryId && 
                               e.Date >= budget.StartDate && 
                               e.Date <= budget.EndDate)
                    .SumAsync(e => e.Amount);

                var percentageUsed = budget.Amount > 0 ? (totalSpent / budget.Amount) * 100 : 0;

                // Check for different notification thresholds
                if (percentageUsed >= 125) // Critical - 125% or more
                {
                    await _notificationService.CreateBudgetCriticalNotificationAsync(
                        userId, budget.Name, budget.Amount, totalSpent, budget.Id);
                }
                else if (percentageUsed >= 100) // Exceeded - 100% or more
                {
                    await _notificationService.CreateBudgetExceededNotificationAsync(
                        userId, budget.Name, budget.Amount, totalSpent, budget.Id);
                }
                else
                {
                    // Check user's warning threshold preference
                    var preferences = await _notificationService.GetUserNotificationPreferencesAsync(userId);
                    if (preferences != null && preferences.EnableBudgetWarnings && 
                        percentageUsed >= preferences.BudgetWarningThreshold)
                    {
                        await _notificationService.CreateBudgetWarningNotificationAsync(
                            userId, budget.Name, budget.Amount, totalSpent, 
                            preferences.BudgetWarningThreshold, budget.Id);
                    }
                }
            }
        }
    }
}