using SmartExpenseTracker.Models;

namespace SmartExpenseTracker.Services
{
    public interface INotificationService
    {
        // Create notifications
        Task CreateBudgetWarningNotificationAsync(string userId, string budgetName, decimal budgetAmount, decimal currentSpent, decimal warningThreshold, int? budgetId = null);
        Task CreateBudgetExceededNotificationAsync(string userId, string budgetName, decimal budgetAmount, decimal currentSpent, int? budgetId = null);
        Task CreateBudgetCriticalNotificationAsync(string userId, string budgetName, decimal budgetAmount, decimal currentSpent, int? budgetId = null);
        Task CreateNewExpenseNotificationAsync(string userId, string expenseTitle, decimal amount, int? expenseId = null);
        Task CreateMonthlyReportNotificationAsync(string userId);
        
        // Get notifications
        Task<List<Notification>> GetUserNotificationsAsync(string userId, int limit = 0);
        Task<int> GetUnreadNotificationCountAsync(string userId);
        Task<Notification?> GetNotificationByIdAsync(int notificationId, string userId);
        
        // Mark notifications as read
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        
        // Delete notifications
        Task DeleteNotificationAsync(int notificationId, string userId);
        Task DeleteOldNotificationsAsync(string userId, int daysOld = 30);
        
        // Check if notification should be sent based on user preferences
        Task<bool> ShouldSendNotificationAsync(string userId, NotificationType type);
        
        // Get or create user notification preferences
        Task<NotificationPreference?> GetUserNotificationPreferencesAsync(string userId);
        Task UpdateUserPreferencesAsync(string userId, NotificationPreference preferences);
    }
}