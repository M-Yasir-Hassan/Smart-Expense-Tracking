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
            var userId = _userManager.GetUserId(User);
            var currentDate = DateTime.Now;
            var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Get all-time data (consistent with Expense/Income Index pages)
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

            // Calculate totals
            var totalExpenses = expenses.Sum(e => e.Amount);
            var totalIncome = income.Sum(i => i.Amount);
            var totalBudget = budgets.Sum(b => b.Amount);
            var netIncome = totalIncome - totalExpenses;

            // Get recent transactions (last 10)
            var recentExpenses = await _context.Expenses
                .Include(e => e.Category)
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .Take(5)
                .ToListAsync();

            var recentIncome = await _context.Incomes
                .Include(i => i.Category)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.Date)
                .Take(5)
                .ToListAsync();

            // Category-wise expense breakdown
            var expensesByCategory = expenses
                .GroupBy(e => e.Category.Name)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    Count = g.Count(),
                    Color = g.First().Category.Color
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // Category-wise income breakdown
            var incomeByCategory = income
                .GroupBy(i => i.Category.Name)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(i => i.Amount),
                    Count = g.Count(),
                    Color = g.First().Category.Color
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // Time period calculations
            var thisMonthStart = new DateTime(currentDate.Year, currentDate.Month, 1);
            var thisMonthEnd = thisMonthStart.AddMonths(1).AddDays(-1);
            
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart.AddDays(-1);
            
            var threeMonthsStart = thisMonthStart.AddMonths(-2);
            var threeMonthsEnd = thisMonthEnd;

            // This Month data
            var thisMonthExpenses = expenses.Where(e => e.Date >= thisMonthStart && e.Date <= thisMonthEnd).ToList();
            var thisMonthIncome = income.Where(i => i.Date >= thisMonthStart && i.Date <= thisMonthEnd).ToList();

            var thisMonthExpensesByCategory = thisMonthExpenses
                .GroupBy(e => e.Category.Name)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    Count = g.Count(),
                    Color = g.First().Category.Color
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            var thisMonthIncomeByCategory = thisMonthIncome
                .GroupBy(i => i.Category.Name)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(i => i.Amount),
                    Count = g.Count(),
                    Color = g.First().Category.Color
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // Last Month data
            var lastMonthExpenses = expenses.Where(e => e.Date >= lastMonthStart && e.Date <= lastMonthEnd).ToList();
            var lastMonthIncomeData = income.Where(i => i.Date >= lastMonthStart && i.Date <= lastMonthEnd).ToList();

            var lastMonthExpensesByCategory = lastMonthExpenses
                .GroupBy(e => e.Category.Name)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    Count = g.Count(),
                    Color = g.First().Category.Color
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            var lastMonthIncomeByCategory = lastMonthIncomeData
                .GroupBy(i => i.Category.Name)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(i => i.Amount),
                    Count = g.Count(),
                    Color = g.First().Category.Color
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // Three Months data
            var threeMonthsExpenses = expenses.Where(e => e.Date >= threeMonthsStart && e.Date <= threeMonthsEnd).ToList();
            var threeMonthsIncomeData = income.Where(i => i.Date >= threeMonthsStart && i.Date <= threeMonthsEnd).ToList();

            var threeMonthsExpensesByCategory = threeMonthsExpenses
                .GroupBy(e => e.Category.Name)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(e => e.Amount),
                    Count = g.Count(),
                    Color = g.First().Category.Color
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            var threeMonthsIncomeByCategory = threeMonthsIncomeData
                .GroupBy(i => i.Category.Name)
                .Select(g => new CategorySummary
                {
                    CategoryName = g.Key,
                    Amount = g.Sum(i => i.Amount),
                    Count = g.Count(),
                    Color = g.First().Category.Color
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            // Budget analysis
            var budgetAnalysis = new List<BudgetAnalysis>();
            foreach (var budget in budgets)
            {
                var budgetExpenses = expenses.Where(e => e.CategoryId == budget.CategoryId).Sum(e => e.Amount);
                var remainingAmount = budget.Amount - budgetExpenses;
                var percentageUsed = budget.Amount > 0 ? (budgetExpenses / budget.Amount) * 100 : 0;

                budgetAnalysis.Add(new BudgetAnalysis
                {
                    BudgetName = budget.Name,
                    BudgetAmount = budget.Amount,
                    SpentAmount = budgetExpenses,
                    RemainingAmount = remainingAmount,
                    PercentageUsed = percentageUsed,
                    CategoryName = budget.Category.Name,
                    CategoryColor = budget.Category.Color,
                    IsOverBudget = budgetExpenses > budget.Amount
                });
            }

            // Last 6 months trend
            var monthlyTrends = new List<MonthlyTrend>();
            for (int i = 5; i >= 0; i--)
            {
                var monthStart = currentDate.AddMonths(-i).Date;
                monthStart = new DateTime(monthStart.Year, monthStart.Month, 1);
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
                    Income = monthIncome,
                    Expenses = monthExpenses,
                    NetIncome = monthIncome - monthExpenses
                });
            }

            // Calculate current month totals
            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;
            var previousMonth = currentMonth == 1 ? 12 : currentMonth - 1;
            var previousYear = currentMonth == 1 ? currentYear - 1 : currentYear;

            var currentMonthIncome = income
                .Where(i => i.Date.Month == currentMonth && i.Date.Year == currentYear)
                .Sum(i => i.Amount);

            var currentMonthExpenses = expenses
                .Where(e => e.Date.Month == currentMonth && e.Date.Year == currentYear)
                .Sum(e => e.Amount);

            var previousMonthIncome = income
                .Where(i => i.Date.Month == previousMonth && i.Date.Year == previousYear)
                .Sum(i => i.Amount);

            var previousMonthExpenses = expenses
                .Where(e => e.Date.Month == previousMonth && e.Date.Year == previousYear)
                .Sum(e => e.Amount);

            var currentMonthNetIncome = currentMonthIncome - currentMonthExpenses;
            var previousMonthNetIncome = previousMonthIncome - previousMonthExpenses;

            // Calculate percentage changes
            double incomeChangePercentage = 0;
            double expenseChangePercentage = 0;
            double netIncomeChangePercentage = 0;

            if (previousMonthIncome > 0)
            {
                incomeChangePercentage = (double)((currentMonthIncome - previousMonthIncome) / previousMonthIncome * 100);
            }
            else if (currentMonthIncome > 0)
            {
                incomeChangePercentage = 100; // 100% increase from 0
            }

            if (previousMonthExpenses > 0)
            {
                expenseChangePercentage = (double)((currentMonthExpenses - previousMonthExpenses) / previousMonthExpenses * 100);
            }
            else if (currentMonthExpenses > 0)
            {
                expenseChangePercentage = 100; // 100% increase from 0
            }

            if (previousMonthNetIncome != 0)
            {
                netIncomeChangePercentage = (double)((currentMonthNetIncome - previousMonthNetIncome) / Math.Abs(previousMonthNetIncome) * 100);
            }
            else if (currentMonthNetIncome != 0)
            {
                netIncomeChangePercentage = currentMonthNetIncome > 0 ? 100 : -100;
            }

            var viewModel = new DashboardViewModel
            {
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                TotalBudget = totalBudget,
                NetIncome = netIncome,
                SavingsRate = totalIncome > 0 ? (double)(((totalIncome - totalExpenses) / totalIncome) * 100) : 0,
                RecentExpenses = recentExpenses,
                RecentIncome = recentIncome,
                ExpensesByCategory = expensesByCategory,
                IncomeByCategory = incomeByCategory,
                BudgetAnalysis = budgetAnalysis,
                MonthlyTrends = monthlyTrends,
                CurrentMonth = currentDate.ToString("MMMM yyyy"),
                
                // Time period specific data
                ThisMonthExpensesByCategory = thisMonthExpensesByCategory,
                LastMonthExpensesByCategory = lastMonthExpensesByCategory,
                ThreeMonthsExpensesByCategory = threeMonthsExpensesByCategory,
                ThisMonthIncomeByCategory = thisMonthIncomeByCategory,
                LastMonthIncomeByCategory = lastMonthIncomeByCategory,
                ThreeMonthsIncomeByCategory = threeMonthsIncomeByCategory,
                
                // Previous month data
                PreviousMonthIncome = previousMonthIncome,
                PreviousMonthExpenses = previousMonthExpenses,
                PreviousMonthNetIncome = previousMonthNetIncome,
                
                // Percentage changes
                IncomeChangePercentage = incomeChangePercentage,
                ExpenseChangePercentage = expenseChangePercentage,
                NetIncomeChangePercentage = netIncomeChangePercentage
            };

            return View(viewModel);
        }
    }
}