using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartExpenseTracker.Models;
using SmartExpenseTracker.Services;

namespace SmartExpenseTracker.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        // GET: Notification
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account");

            var notifications = await _notificationService.GetUserNotificationsAsync(userId);
            return View(notifications);
        }

        // GET: Notification/Unread
        public async Task<IActionResult> Unread()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account");

            // Get all notifications and filter unread ones in memory for now
            var allNotifications = await _notificationService.GetUserNotificationsAsync(userId);
            var notifications = allNotifications.Where(n => !n.IsRead).ToList();
            return View("Index", notifications);
        }

        // POST: Notification/MarkAsRead/5
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account");

            await _notificationService.MarkAsReadAsync(id, userId);
            return Json(new { success = true });
        }

        // POST: Notification/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account");

            await _notificationService.MarkAllAsReadAsync(userId);
            TempData["SuccessMessage"] = "All notifications marked as read.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            await _notificationService.DeleteNotificationAsync(id, userId);
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent(int limit = 5)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, limit);
            
            // Format notifications for JavaScript consumption
            var formattedNotifications = notifications.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                notificationType = n.Type.ToString(),
                priority = n.Priority.ToString(),
                isRead = n.IsRead,
                createdAt = n.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), // ISO format for JavaScript
                readAt = n.ReadAt?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                budgetId = n.BudgetId,
                expenseId = n.ExpenseId
            });
            
            return Json(formattedNotifications);
        }

        // GET: Notification/GetUnreadCount
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Json(new { count = 0 });

            var count = await _notificationService.GetUnreadNotificationCountAsync(userId);
            return Json(new { count });
        }

        // GET: Notification/Preferences
        public async Task<IActionResult> Preferences()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account");

            var preferences = await _notificationService.GetUserNotificationPreferencesAsync(userId);
            if (preferences == null)
            {
                // Create default preferences if none exist
                preferences = new NotificationPreference { UserId = userId };
            }
            return View(preferences);
        }

        // POST: Notification/Preferences
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preferences(NotificationPreference model)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                await _notificationService.UpdateUserPreferencesAsync(userId, model);
                TempData["SuccessMessage"] = "Notification preferences updated successfully.";
                return RedirectToAction(nameof(Preferences));
            }

            return View(model);
        }

        // GET: Notification/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToAction("Login", "Account");

            var notification = await _notificationService.GetNotificationByIdAsync(id, userId);
            if (notification == null)
            {
                return NotFound();
            }

            // Mark as read when viewing details
            if (!notification.IsRead)
            {
                await _notificationService.MarkAsReadAsync(id, userId);
            }

            return View(notification);
        }
    }
}