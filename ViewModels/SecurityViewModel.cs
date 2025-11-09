using System.ComponentModel.DataAnnotations;

namespace SmartExpenseTracker.ViewModels
{
    public class SecurityViewModel
    {
        public string UserEmail { get; set; } = string.Empty;
        public DateTime AccountCreatedDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public int TotalExpenses { get; set; }
        public int TotalIncome { get; set; }
        public int TotalBudgets { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool EmailConfirmed { get; set; }
    }

    public class ExportDataViewModel
    {
        [Display(Name = "Export Format")]
        public string Format { get; set; } = "json";
        
        [Display(Name = "Include Expenses")]
        public bool IncludeExpenses { get; set; } = true;
        
        [Display(Name = "Include Income")]
        public bool IncludeIncome { get; set; } = true;
        
        [Display(Name = "Include Budgets")]
        public bool IncludeBudgets { get; set; } = true;
        
        [Display(Name = "Date Range From")]
        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }
        
        [Display(Name = "Date Range To")]
        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }
    }
}