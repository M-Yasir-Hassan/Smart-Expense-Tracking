using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SmartExpenseTracker.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}";

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = false;

        public DateTime? ApprovedDate { get; set; }

        public string? ApprovedByUserId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? LastLoginDate { get; set; }

        public string? ProfilePictureUrl { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        public virtual ApplicationUser? ApprovedBy { get; set; }
        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public virtual ICollection<Income> Incomes { get; set; } = new List<Income>();
        public virtual ICollection<Budget> Budgets { get; set; } = new List<Budget>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual NotificationPreference? NotificationPreference { get; set; }
    }
}
