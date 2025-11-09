using System.ComponentModel.DataAnnotations;

namespace SmartExpenseTracker.Models
{
    public class Notification
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;
        
        [Required]
        public NotificationType Type { get; set; }
        
        public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;
        
        public bool IsRead { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ReadAt { get; set; }
        
        // Related entity IDs for context
        public int? BudgetId { get; set; }
        public int? ExpenseId { get; set; }
        
        // Navigation properties
        public Budget? Budget { get; set; }
        public Expense? Expense { get; set; }
    }
    
    public enum NotificationType
    {
        BudgetWarning,      // 75% of budget reached
        BudgetExceeded,     // 100% of budget exceeded
        BudgetCritical,     // 125% of budget exceeded
        ExpenseAdded,       // New expense added
        BudgetCreated,      // New budget created
        MonthlyReport       // Monthly summary
    }
    
    public enum NotificationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}