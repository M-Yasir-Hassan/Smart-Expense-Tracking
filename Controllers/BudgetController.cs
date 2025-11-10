using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SmartExpenseTracker.Data;
using SmartExpenseTracker.Models;

namespace SmartExpenseTracker.Controllers
{
    [Authorize]
    public class BudgetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BudgetController> _logger;

        public BudgetController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<BudgetController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Budget
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var budgets = await _context.Budgets
                .Include(b => b.Category)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();

            // Calculate spent amounts for each budget
            foreach (var budget in budgets)
            {
                var spentAmount = await _context.Expenses
                    .Where(e => e.UserId == userId && 
                               e.CategoryId == budget.CategoryId && 
                               e.Date >= budget.StartDate && 
                               e.Date <= budget.EndDate)
                    .SumAsync(e => e.Amount);
                
                budget.SpentAmount = spentAmount;
            }

            ViewBag.TotalBudget = budgets.Sum(b => b.Amount);
            ViewBag.TotalSpent = budgets.Sum(b => b.SpentAmount);
            return View(budgets);
        }

        // GET: Budget/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var budget = await _context.Budgets
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (budget == null)
            {
                return NotFound();
            }

            // Calculate spent amount
            var spentAmount = await _context.Expenses
                .Where(e => e.UserId == userId && 
                           e.CategoryId == budget.CategoryId && 
                           e.Date >= budget.StartDate && 
                           e.Date <= budget.EndDate)
                .SumAsync(e => e.Amount);
            
            budget.SpentAmount = spentAmount;

            // Get related expenses
            ViewBag.RelatedExpenses = await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && 
                           e.CategoryId == budget.CategoryId && 
                           e.Date >= budget.StartDate && 
                           e.Date <= budget.EndDate)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            return View(budget);
        }

        // GET: Budget/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            var budget = new Budget
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1),
                IsActive = true
            };
            return View(budget);
        }

        // POST: Budget/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Amount,StartDate,EndDate,CategoryId,IsActive")] Budget budget)
        {
            _logger.LogInformation("Budget Create POST method called");
            _logger.LogInformation("Budget data received: Name={Name}, Amount={Amount}, StartDate={StartDate}, EndDate={EndDate}, CategoryId={CategoryId}, IsActive={IsActive}", 
                budget.Name, budget.Amount, budget.StartDate, budget.EndDate, budget.CategoryId, budget.IsActive);

            try
            {
                // Set UserId before validation
                budget.UserId = _userManager.GetUserId(User);
                budget.CreatedDate = DateTime.Now;
                budget.ModifiedDate = DateTime.Now;
                
                _logger.LogInformation("Budget prepared with UserId={UserId} before validation", budget.UserId);

                if (ModelState.IsValid)
                {
                    _logger.LogInformation("ModelState is valid, proceeding with budget creation");
                    
                    _logger.LogInformation("Budget prepared for saving: UserId={UserId}, CreatedDate={CreatedDate}", 
                        budget.UserId, budget.CreatedDate);
                    
                    _context.Add(budget);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Budget created successfully with ID={BudgetId}", budget.Id);
                    TempData["SuccessMessage"] = "Budget created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogWarning("ModelState is invalid. Validation errors:");
                    foreach (var modelError in ModelState)
                    {
                        foreach (var error in modelError.Value.Errors)
                        {
                            _logger.LogWarning("Field: {Field}, Error: {Error}", modelError.Key, error.ErrorMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating budget");
                TempData["ErrorMessage"] = "An error occurred while creating the budget. Please try again.";
            }

            _logger.LogInformation("Returning to Create view with validation errors");
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", budget.CategoryId);
            return View(budget);
        }

        // GET: Budget/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            
            if (budget == null)
            {
                return NotFound();
            }
            
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", budget.CategoryId);
            return View(budget);
        }

        // POST: Budget/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Amount,StartDate,EndDate,CategoryId,UserId,IsActive,CreatedDate")] Budget budget)
        {
            if (id != budget.Id)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (budget.UserId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    budget.ModifiedDate = DateTime.Now;
                    _context.Update(budget);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Budget updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BudgetExists(budget.Id))
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
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", budget.CategoryId);
            return View(budget);
        }

        // GET: Budget/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var budget = await _context.Budgets
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (budget == null)
            {
                return NotFound();
            }

            // Calculate spent amount
            var spentAmount = await _context.Expenses
                .Where(e => e.UserId == userId && 
                           e.CategoryId == budget.CategoryId && 
                           e.Date >= budget.StartDate && 
                           e.Date <= budget.EndDate)
                .SumAsync(e => e.Amount);
            
            budget.SpentAmount = spentAmount;

            return View(budget);
        }

        // POST: Budget/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var budget = await _context.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);
            
            if (budget != null)
            {
                _context.Budgets.Remove(budget);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Budget deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BudgetExists(int id)
        {
            return _context.Budgets.Any(e => e.Id == id);
        }
    }
}