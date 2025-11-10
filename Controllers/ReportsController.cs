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
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var currentDate = DateTime.Now;
            var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Get all-time data (consistent with Dashboard and other Index pages)
            var expenses = await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId)
                .ToListAsync();

            var income = await _context.Incomes
                .Include(i => i.Category)
                .Where(i => i.UserId == userId)
                .ToListAsync();

            var budgets = await _context.Budgets
                .Include(b => b.Category)
                .Where(b => b.UserId == userId && b.IsActive)
                .ToListAsync();

            // Calculate summary statistics
            var totalExpenses = expenses.Sum(e => e.Amount);
            var totalIncome = income.Sum(i => i.Amount);
            var totalBudget = budgets.Sum(b => b.Amount);
            var netIncome = totalIncome - totalExpenses;

            // Category-wise expense breakdown
            var expensesByCategory = expenses
                .GroupBy(e => e.Category)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key.Name,
                    Amount = g.Sum(e => e.Amount),
                    Count = g.Count(),
                    Color = g.Key.Color,
                    Percentage = totalExpenses > 0 ? (double)(g.Sum(e => e.Amount) / totalExpenses * 100) : 0
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // Category-wise income breakdown
            var incomeByCategory = income
                .GroupBy(i => i.Category)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key.Name,
                    Amount = g.Sum(i => i.Amount),
                    Count = g.Count(),
                    Color = g.Key.Color,
                    Percentage = totalIncome > 0 ? (double)(g.Sum(i => i.Amount) / totalIncome * 100) : 0
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // Budget vs Actual spending
            var budgetAnalysis = new List<BudgetAnalysis>();
            foreach (var budget in budgets)
            {
                var spent = expenses
                    .Where(e => e.CategoryId == budget.CategoryId && e.Date >= budget.StartDate && e.Date <= budget.EndDate)
                    .Sum(e => e.Amount);

                budgetAnalysis.Add(new BudgetAnalysis
                {
                    BudgetName = budget.Name,
                    BudgetAmount = budget.Amount,
                    SpentAmount = spent,
                    RemainingAmount = budget.Amount - spent,
                    PercentageUsed = budget.Amount > 0 ? (spent / budget.Amount * 100) : 0,
                    CategoryName = budget.Category.Name,
                    CategoryColor = budget.Category.Color,
                    IsOverBudget = spent > budget.Amount
                });
            }

            // Monthly trend data (last 6 months)
            var monthlyTrends = new List<MonthlyTrend>();
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = startOfMonth.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                var monthExpenses = await _context.Expenses
                    .Where(e => e.UserId == userId && e.Date >= monthStart && e.Date <= monthEnd)
                    .SumAsync(e => e.Amount);

                var monthIncome = await _context.Incomes
                    .Where(i => i.UserId == userId && i.Date >= monthStart && i.Date <= monthEnd)
                    .SumAsync(i => i.Amount);

                monthlyTrends.Add(new MonthlyTrend
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    Expenses = monthExpenses,
                    Income = monthIncome,
                    NetIncome = monthIncome - monthExpenses
                });
            }

            var viewModel = new ReportsViewModel
            {
                CurrentMonth = currentDate.ToString("MMMM yyyy"),
                TotalExpenses = totalExpenses,
                TotalIncome = totalIncome,
                TotalBudget = totalBudget,
                NetIncome = netIncome,
                ExpensesByCategory = expensesByCategory,
                IncomeByCategory = incomeByCategory,
                BudgetAnalysis = budgetAnalysis,
                MonthlyTrends = monthlyTrends,
                SavingsRate = totalIncome > 0 ? (double)(netIncome / totalIncome * 100) : 0
            };

            return View(viewModel);
        }

        // GET: Reports/Detailed
        public async Task<IActionResult> Detailed(DateTime? startDate, DateTime? endDate, int? categoryId)
        {
            var userId = _userManager.GetUserId(User);
            
            // Default to current month if no dates provided
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var currentDate = DateTime.Now;
                startDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
            }

            var expensesQuery = _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate);

            var incomeQuery = _context.Incomes
                .Include(i => i.Category)
                .Where(i => i.UserId == userId && i.Date >= startDate && i.Date <= endDate);

            if (categoryId.HasValue)
            {
                expensesQuery = expensesQuery.Where(e => e.CategoryId == categoryId);
                incomeQuery = incomeQuery.Where(i => i.CategoryId == categoryId);
            }

            var expenses = await expensesQuery.OrderByDescending(e => e.Date).ToListAsync();
            var income = await incomeQuery.OrderByDescending(i => i.Date).ToListAsync();
            var categories = await _context.Categories.ToListAsync();

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            ViewBag.CategoryId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(categories, "Id", "Name", categoryId);
            ViewBag.SelectedCategoryId = categoryId;

            var viewModel = new DetailedReportsViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                Expenses = expenses,
                Income = income,
                TotalExpenses = expenses.Sum(e => e.Amount),
                TotalIncome = income.Sum(i => i.Amount)
            };

            return View(viewModel);
        }

        // GET: Reports/Export
        public async Task<IActionResult> Export(DateTime? startDate, DateTime? endDate, string format = "csv")
        {
            var userId = _userManager.GetUserId(User);
            
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var currentDate = DateTime.Now;
                startDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
            }

            var expenses = await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId && e.Date >= startDate && e.Date <= endDate)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            var income = await _context.Incomes
                .Include(i => i.Category)
                .Where(i => i.UserId == userId && i.Date >= startDate && i.Date <= endDate)
                .OrderByDescending(i => i.Date)
                .ToListAsync();

            if (format.ToLower() == "csv")
            {
                return ExportToCsv(expenses, income, startDate.Value, endDate.Value);
            }

            return RedirectToAction(nameof(Index));
        }

        private IActionResult ExportToCsv(List<Expense> expenses, List<Income> income, DateTime startDate, DateTime endDate)
        {
            var csv = new System.Text.StringBuilder();
            
            // Header
            csv.AppendLine("Smart Expense Tracker - Financial Report");
            csv.AppendLine($"Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            csv.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();
            
            // Summary
            csv.AppendLine("SUMMARY");
            csv.AppendLine($"Total Expenses,{expenses.Sum(e => e.Amount):C}");
            csv.AppendLine($"Total Income,{income.Sum(i => i.Amount):C}");
            csv.AppendLine($"Net Income,{income.Sum(i => i.Amount) - expenses.Sum(e => e.Amount):C}");
            csv.AppendLine();
            
            // Expenses
            csv.AppendLine("EXPENSES");
            csv.AppendLine("Date,Title,Category,Amount,Description");
            foreach (var expense in expenses)
            {
                csv.AppendLine($"{expense.Date:yyyy-MM-dd},\"{expense.Title}\",\"{expense.Category.Name}\",{expense.Amount},\"{expense.Description}\"");
            }
            csv.AppendLine();
            
            // Income
            csv.AppendLine("INCOME");
            csv.AppendLine("Date,Title,Category,Amount,Description");
            foreach (var incomeItem in income)
            {
                csv.AppendLine($"{incomeItem.Date:yyyy-MM-dd},\"{incomeItem.Title}\",\"{incomeItem.Category.Name}\",{incomeItem.Amount},\"{incomeItem.Description}\"");
            }

            var fileName = $"financial-report-{startDate:yyyy-MM-dd}-to-{endDate:yyyy-MM-dd}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }
    }
}