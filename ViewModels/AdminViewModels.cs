using SmartExpenseTracker.Models;
using System.ComponentModel.DataAnnotations;

namespace SmartExpenseTracker.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int PendingApprovals { get; set; }
        public int ApprovedUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalExpenses { get; set; }
        public int TotalIncome { get; set; }
        public int TotalBudgets { get; set; }
        public decimal TotalExpenseAmount { get; set; }
        public decimal TotalIncomeAmount { get; set; }
        public List<RecentUserRegistration> RecentRegistrations { get; set; } = new();
        public List<UserActivitySummary> UserActivities { get; set; } = new();
        public Dictionary<string, int> MonthlyRegistrations { get; set; } = new();
        public Dictionary<string, decimal> MonthlyExpenses { get; set; } = new();
    }

    public class RecentUserRegistration
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public bool IsApproved { get; set; }
    }

    public class UserActivitySummary
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? LastLoginDate { get; set; }
        public int ExpenseCount { get; set; }
        public int IncomeCount { get; set; }
        public int BudgetCount { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal TotalIncome { get; set; }
    }

    public class UserManagementViewModel
    {
        public List<UserListItem> Users { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string StatusFilter { get; set; } = "All";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalUsers => Users.Count;
    }

    public class UserListItem
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public int ExpenseCount { get; set; }
        public int IncomeCount { get; set; }
        public int BudgetCount { get; set; }
    }

    public class UserDetailsViewModel
    {
        public ApplicationUser User { get; set; } = new();
        public List<string> Roles { get; set; } = new();
        public UserStatistics Statistics { get; set; } = new();
        public List<RecentActivity> RecentActivities { get; set; } = new();
    }

    public class UserStatistics
    {
        public int TotalExpenses { get; set; }
        public int TotalIncome { get; set; }
        public int TotalBudgets { get; set; }
        public decimal TotalExpenseAmount { get; set; }
        public decimal TotalIncomeAmount { get; set; }
        public decimal CurrentMonthExpenses { get; set; }
        public decimal CurrentMonthIncome { get; set; }
    }

    public class RecentActivity
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Is Approved")]
        public bool IsApproved { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        public string? Notes { get; set; }

        [Display(Name = "Roles")]
        public List<string> SelectedRoles { get; set; } = new();

        public List<string> AvailableRoles { get; set; } = new();
    }
}
