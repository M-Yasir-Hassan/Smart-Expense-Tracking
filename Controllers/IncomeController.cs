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
    public class IncomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public IncomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Income
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var incomes = await _context.Incomes
                .Include(i => i.Category)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            ViewBag.TotalIncome = incomes.Sum(i => i.Amount);
            return View(incomes);
        }

        // GET: Income/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var income = await _context.Incomes
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (income == null)
            {
                return NotFound();
            }

            return View(income);
        }

        // GET: Income/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            var income = new Income
            {
                Date = DateTime.Today
            };
            return View(income);
        }

        // POST: Income/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Amount,Date,CategoryId")] Income income)
        {
            // Set UserId before validation
            income.UserId = _userManager.GetUserId(User) ?? string.Empty;
            income.CreatedDate = DateTime.Now;
            income.ModifiedDate = DateTime.Now;
            
            // Debug: Log ModelState errors
            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState)
                {
                    foreach (var error in modelError.Value.Errors)
                    {
                        Console.WriteLine($"ModelState Error - Key: {modelError.Key}, Error: {error.ErrorMessage}");
                    }
                }
            }
            
            if (ModelState.IsValid)
            {
                _context.Add(income);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Income added successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", income.CategoryId);
            return View(income);
        }

        // GET: Income/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
            
            if (income == null)
            {
                return NotFound();
            }
            
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", income.CategoryId);
            return View(income);
        }

        // POST: Income/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Amount,Date,CategoryId,UserId,CreatedDate")] Income income)
        {
            if (id != income.Id)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (income.UserId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    income.ModifiedDate = DateTime.Now;
                    _context.Update(income);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Income updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IncomeExists(income.Id))
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
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", income.CategoryId);
            return View(income);
        }

        // GET: Income/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var income = await _context.Incomes
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

            if (income == null)
            {
                return NotFound();
            }

            return View(income);
        }

        // POST: Income/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _userManager.GetUserId(User);
            var income = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
            
            if (income != null)
            {
                _context.Incomes.Remove(income);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Income deleted successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool IncomeExists(int id)
        {
            return _context.Incomes.Any(e => e.Id == id);
        }
    }
}