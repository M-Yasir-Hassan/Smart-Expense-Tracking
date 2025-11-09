using SmartExpenseTracker.Models;

namespace SmartExpenseTracker.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal NetIncome { get; set; }
        public double SavingsRate { get; set; }
        public string CurrentMonth { get; set; } = string.Empty;
        
        public List<Expense> RecentExpenses { get; set; } = new List<Expense>();
        public List<Income> RecentIncome { get; set; } = new List<Income>();
        
        public List<CategorySummary> ExpensesByCategory { get; set; } = new List<CategorySummary>();
        public List<CategorySummary> IncomeByCategory { get; set; } = new List<CategorySummary>();
        
        public List<BudgetAnalysis> BudgetAnalysis { get; set; } = new List<BudgetAnalysis>();
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new List<MonthlyTrend>();
    }

    public class CategorySummary
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
        public string Color { get; set; } = string.Empty;
        public double Percentage { get; set; }
    }

    public class BudgetAnalysis
    {
        public string BudgetName { get; set; } = string.Empty;
        public decimal BudgetAmount { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal PercentageUsed { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryColor { get; set; } = string.Empty;
        public bool IsOverBudget { get; set; }
        public string StatusClass => IsOverBudget ? "danger" : PercentageUsed >= 80 ? "warning" : "success";
        public string StatusText => IsOverBudget ? "Over Budget" : PercentageUsed >= 80 ? "Near Limit" : "On Track";
    }

    public class MonthlyTrend
    {
        public string Month { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expenses { get; set; }
        public decimal NetIncome { get; set; }
    }
}