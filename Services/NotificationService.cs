using Microsoft.EntityFrameworkCore;
using SmartExpenseTracker.Data;
using SmartExpenseTracker.Models;

namespace SmartExpenseTracker.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateBudgetWarningNotificationAsync(string userId, string budgetName, decimal budgetAmount, decimal currentSpent, decimal warningThreshold, int? budgetId = null)
        {
            if (!await ShouldSendNotificationAsync(userId, NotificationType.BudgetWarning))
                return;

            var percentage = Math.Round((currentSpent / budgetAmount) * 100, 1);
            
            var notification = new Notification
            {
                UserId = userId,
                Title = "Budget Warning",
                Message = $"You've spent {percentage}% of your {budgetName} budget. Current spending: ${currentSpent:F2} of ${budgetAmount:F2}",
                Type = NotificationType.BudgetWarning,
                Priority = NotificationPriority.Medium,
                BudgetId = budgetId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateBudgetExceededNotificationAsync(string userId, string budgetName, decimal budgetAmount, decimal currentSpent, int? budgetId = null)
        {
            if (!await ShouldSendNotificationAsync(userId, NotificationType.BudgetExceeded))
                return;

            var overspent = currentSpent - budgetAmount;
            
            var notification = new Notification
            {
                UserId = userId,
                Title = "Budget Exceeded!",
                Message = $"You've exceeded your {budgetName} budget by ${overspent:F2}. Total spent: ${currentSpent:F2} (Budget: ${budgetAmount:F2})",
                Type = NotificationType.BudgetExceeded,
                Priority = NotificationPriority.High,
                BudgetId = budgetId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateBudgetCriticalNotificationAsync(string userId, string budgetName, decimal budgetAmount, decimal currentSpent, int? budgetId = null)
        {
            if (!await ShouldSendNotificationAsync(userId, NotificationType.BudgetCritical))
                return;

            var percentage = Math.Round((currentSpent / budgetAmount) * 100, 1);
            var overspent = currentSpent - budgetAmount;
            
            var notification = new Notification
            {
                UserId = userId,
                Title = "Critical Budget Alert!",
                Message = $"URGENT: You've spent {percentage}% of your {budgetName} budget! You're ${overspent:F2} over budget. Immediate action recommended.",
                Type = NotificationType.BudgetCritical,
                Priority = NotificationPriority.Critical,
                BudgetId = budgetId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateNewExpenseNotificationAsync(string userId, string expenseTitle, decimal amount, int? expenseId = null)
        {
            if (!await ShouldSendNotificationAsync(userId, NotificationType.ExpenseAdded))
                return;

            var notification = new Notification
            {
                UserId = userId,
                Title = "New Expense Added",
                Message = $"Expense '{expenseTitle}' of ${amount:F2} has been added",
                Type = NotificationType.ExpenseAdded,
                Priority = NotificationPriority.Low,
                ExpenseId = expenseId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateMonthlyReportNotificationAsync(string userId)
        {
            if (!await ShouldSendNotificationAsync(userId, NotificationType.MonthlyReport))
                return;

            var notification = new Notification
            {
                UserId = userId,
                Title = "Monthly Financial Report",
                Message = "Your monthly financial report is ready. Check your dashboard for detailed insights.",
                Type = NotificationType.MonthlyReport,
                Priority = NotificationPriority.Medium
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int limit = 0)
        {
            var query = _context.Notifications
                .Include(n => n.Budget)
                    .ThenInclude(b => b!.Category)
                .Include(n => n.Expense)
                    .ThenInclude(e => e!.Category)
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            if (limit > 0)
                return await query.Take(limit).ToListAsync();

            return await query.ToListAsync();
        }

        public async Task<int> GetUnreadNotificationCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<Notification?> GetNotificationByIdAsync(int notificationId, string userId)
        {
            return await _context.Notifications
                .Include(n => n.Budget)
                    .ThenInclude(b => b!.Category)
                .Include(n => n.Expense)
                    .ThenInclude(e => e!.Category)
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteOldNotificationsAsync(string userId, int daysOld = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var oldNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.CreatedAt < cutoffDate)
                .ToListAsync();

            _context.Notifications.RemoveRange(oldNotifications);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ShouldSendNotificationAsync(string userId, NotificationType type)
        {
            var preferences = await GetUserNotificationPreferencesAsync(userId);
            
            // Check if it's quiet hours
            if (preferences != null && preferences.EnableQuietHours && IsQuietHours(preferences))
                return false;

            if (preferences == null)
                return true; // Default to sending notifications if no preferences set

            return type switch
            {
                NotificationType.BudgetWarning => preferences.EnableBudgetWarnings,
                NotificationType.BudgetExceeded => preferences.EnableBudgetExceededAlerts,
                NotificationType.BudgetCritical => preferences.EnableBudgetCriticalAlerts,
                NotificationType.ExpenseAdded => preferences.EnableExpenseNotifications,
                NotificationType.MonthlyReport => preferences.EnableMonthlyReports,
                _ => true
            };
        }

        public async Task<NotificationPreference?> GetUserNotificationPreferencesAsync(string userId)
        {
            return await _context.NotificationPreferences
                .FirstOrDefaultAsync(np => np.UserId == userId);
        }

        public async Task UpdateUserPreferencesAsync(string userId, NotificationPreference preferences)
        {
            var existingPreferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(np => np.UserId == userId);

            if (existingPreferences != null)
            {
                existingPreferences.EnableBudgetWarnings = preferences.EnableBudgetWarnings;
                existingPreferences.BudgetWarningThreshold = preferences.BudgetWarningThreshold;
                existingPreferences.EnableBudgetExceededAlerts = preferences.EnableBudgetExceededAlerts;
                existingPreferences.EnableBudgetCriticalAlerts = preferences.EnableBudgetCriticalAlerts;
                existingPreferences.EnableExpenseNotifications = preferences.EnableExpenseNotifications;
                existingPreferences.EnableMonthlyReports = preferences.EnableMonthlyReports;
                existingPreferences.EnableInAppNotifications = preferences.EnableInAppNotifications;
                existingPreferences.EnableEmailNotifications = preferences.EnableEmailNotifications;
                existingPreferences.EnableQuietHours = preferences.EnableQuietHours;
                existingPreferences.QuietHoursStart = preferences.QuietHoursStart;
                existingPreferences.QuietHoursEnd = preferences.QuietHoursEnd;
                existingPreferences.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                preferences.UserId = userId;
                _context.NotificationPreferences.Add(preferences);
            }

            await _context.SaveChangesAsync();
        }

        private bool IsQuietHours(NotificationPreference preferences)
        {
            var now = DateTime.Now.TimeOfDay;
            var start = preferences.QuietHoursStart;
            var end = preferences.QuietHoursEnd;

            if (start < end)
            {
                // Same day quiet hours (e.g., 10 PM to 11 PM)
                return now >= start && now <= end;
            }
            else
            {
                // Overnight quiet hours (e.g., 10 PM to 8 AM)
                return now >= start || now <= end;
            }
        }
    }
}