using SmartExpenseTracker.Models;

namespace SmartExpenseTracker.ViewModels
{
    public class ReportsViewModel
    {
        public string CurrentMonth { get; set; } = string.Empty;
        public decimal TotalExpenses { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalBudget { get; set; }
        public decimal NetIncome { get; set; }
        public double SavingsRate { get; set; }
        public List<CategorySummary> ExpensesByCategory { get; set; } = new List<CategorySummary>();
        public List<CategorySummary> IncomeByCategory { get; set; } = new List<CategorySummary>();
        public List<BudgetAnalysis> BudgetAnalysis { get; set; } = new List<BudgetAnalysis>();
        public List<MonthlyTrend> MonthlyTrends { get; set; } = new List<MonthlyTrend>();
    }

    public class DetailedReportsViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<Expense> Expenses { get; set; } = new List<Expense>();
        public List<Income> Income { get; set; } = new List<Income>();
        public decimal TotalExpenses { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal NetIncome => TotalIncome - TotalExpenses;
    }

    public class ExpenseSummary
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalExpenses { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal NetIncome => TotalIncome - TotalExpenses;
    }
}