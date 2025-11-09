using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartExpenseTracker.Models;

namespace SmartExpenseTracker.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<Income> Incomes { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure decimal precision
        modelBuilder.Entity<Expense>()
            .Property(e => e.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Income>()
            .Property(i => i.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Budget>()
            .Property(b => b.Amount)
            .HasPrecision(18, 2);

        // Configure relationships
        modelBuilder.Entity<Expense>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Income>()
            .HasOne(i => i.Category)
            .WithMany(c => c.Incomes)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Budget>()
            .HasOne(b => b.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Notification relationships
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Budget)
            .WithMany()
            .HasForeignKey(n => n.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Expense)
            .WithMany()
            .HasForeignKey(n => n.ExpenseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure NotificationPreference - one per user
        modelBuilder.Entity<NotificationPreference>()
            .HasIndex(np => np.UserId)
            .IsUnique();

        // Seed default categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Food & Dining", Description = "Restaurants, groceries, and food delivery", Color = "#FF6B6B", Icon = "fas fa-utensils", CreatedDate = new DateTime(2024, 1, 1) },
            new Category { Id = 2, Name = "Transportation", Description = "Gas, public transport, car maintenance", Color = "#4ECDC4", Icon = "fas fa-car", CreatedDate = new DateTime(2024, 1, 1) },
            new Category { Id = 3, Name = "Shopping", Description = "Clothing, electronics, and general shopping", Color = "#45B7D1", Icon = "fas fa-shopping-bag", CreatedDate = new DateTime(2024, 1, 1) },
            new Category { Id = 4, Name = "Entertainment", Description = "Movies, games, and recreational activities", Color = "#96CEB4", Icon = "fas fa-gamepad", CreatedDate = new DateTime(2024, 1, 1) },
            new Category { Id = 5, Name = "Bills & Utilities", Description = "Electricity, water, internet, phone bills", Color = "#FFEAA7", Icon = "fas fa-file-invoice", CreatedDate = new DateTime(2024, 1, 1) },
            new Category { Id = 6, Name = "Healthcare", Description = "Medical expenses, pharmacy, insurance", Color = "#DDA0DD", Icon = "fas fa-heartbeat", CreatedDate = new DateTime(2024, 1, 1) },
            new Category { Id = 7, Name = "Education", Description = "Books, courses, tuition fees", Color = "#98D8C8", Icon = "fas fa-graduation-cap", CreatedDate = new DateTime(2024, 1, 1) },
            new Category { Id = 8, Name = "Salary", Description = "Monthly salary and wages", Color = "#6C5CE7", Icon = "fas fa-money-bill-wave", CreatedDate = new DateTime(2024, 1, 1) },
            new Category { Id = 9, Name = "Freelance", Description = "Freelance work and side projects", Color = "#A29BFE", Icon = "fas fa-laptop-code", CreatedDate = new DateTime(2024, 1, 1) },
            new Category { Id = 10, Name = "Investment", Description = "Dividends, interest, and investment returns", Color = "#FD79A8", Icon = "fas fa-chart-line", CreatedDate = new DateTime(2024, 1, 1) }
        );
    }
}
