using System.ComponentModel.DataAnnotations;

namespace SmartExpenseTracker.Models
{
    public class NotificationPreference
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        // Budget notification settings
        public bool EnableBudgetWarnings { get; set; } = true;
        public int BudgetWarningThreshold { get; set; } = 75; // Percentage
        
        public bool EnableBudgetExceededAlerts { get; set; } = true;
        public bool EnableBudgetCriticalAlerts { get; set; } = true;
        
        // Other notification settings
        public bool EnableExpenseNotifications { get; set; } = false;
        public bool EnableMonthlyReports { get; set; } = true;
        
        // Notification delivery preferences
        public bool EnableInAppNotifications { get; set; } = true;
        public bool EnableEmailNotifications { get; set; } = false;
        
        // Quiet hours
        public bool EnableQuietHours { get; set; } = false;
        public TimeSpan QuietHoursStart { get; set; } = new TimeSpan(22, 0, 0); // 10 PM
        public TimeSpan QuietHoursEnd { get; set; } = new TimeSpan(8, 0, 0);   // 8 AM
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}