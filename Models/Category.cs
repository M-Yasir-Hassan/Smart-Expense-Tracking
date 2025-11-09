using System.ComponentModel.DataAnnotations;

namespace SmartExpenseTracker.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [Required]
        [StringLength(7)]
        public string Color { get; set; } = "#007bff"; // Default blue color

        [Required]
        public string Icon { get; set; } = "fas fa-tag"; // Default icon

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
        public virtual ICollection<Income> Incomes { get; set; } = new List<Income>();
    }
}