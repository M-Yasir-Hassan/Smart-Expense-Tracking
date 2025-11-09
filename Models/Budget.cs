using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SmartExpenseTracker.Models
{
    public class Budget
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Budget amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }

        // Calculated property for spent amount
        [NotMapped]
        public decimal SpentAmount { get; set; }

        [NotMapped]
        public decimal RemainingAmount => Amount - SpentAmount;

        [NotMapped]
        public double PercentageUsed => Amount > 0 ? (double)(SpentAmount / Amount) * 100 : 0;
    }
}