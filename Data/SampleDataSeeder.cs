using Microsoft.AspNetCore.Identity;
using SmartExpenseTracker.Models;

namespace SmartExpenseTracker.Data
{
    public static class SampleDataSeeder
    {
        public static async Task SeedSampleDataAsync(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            // Check if sample data already exists
            if (context.Expenses.Any() || context.Incomes.Any() || context.Budgets.Any())
            {
                return; // Sample data already exists
            }

            // Create sample categories
            var categories = new[]
            {
                new Category { Name = "Food & Dining", Description = "Restaurants, groceries, and food expenses", Color = "#FF6B6B", Icon = "fas fa-utensils" },
                new Category { Name = "Transportation", Description = "Gas, public transport, and vehicle expenses", Color = "#4ECDC4", Icon = "fas fa-car" },
                new Category { Name = "Shopping", Description = "Clothing, electronics, and general shopping", Color = "#45B7D1", Icon = "fas fa-shopping-bag" },
                new Category { Name = "Entertainment", Description = "Movies, games, and entertainment expenses", Color = "#96CEB4", Icon = "fas fa-gamepad" },
                new Category { Name = "Bills & Utilities", Description = "Electricity, water, internet, and other bills", Color = "#FFEAA7", Icon = "fas fa-file-invoice" },
                new Category { Name = "Healthcare", Description = "Medical expenses and health-related costs", Color = "#DDA0DD", Icon = "fas fa-heartbeat" },
                new Category { Name = "Education", Description = "Books, courses, and educational expenses", Color = "#98D8C8", Icon = "fas fa-graduation-cap" },
                new Category { Name = "Travel", Description = "Vacation and travel-related expenses", Color = "#F7DC6F", Icon = "fas fa-plane" },
                new Category { Name = "Salary", Description = "Monthly salary and wages", Color = "#82E0AA", Icon = "fas fa-money-bill-wave" },
                new Category { Name = "Freelance", Description = "Freelance work and contract income", Color = "#85C1E9", Icon = "fas fa-laptop" },
                new Category { Name = "Investment", Description = "Returns from investments and dividends", Color = "#F8C471", Icon = "fas fa-chart-line" },
                new Category { Name = "Business", Description = "Business income and profits", Color = "#D7BDE2", Icon = "fas fa-briefcase" },
                new Category { Name = "Other Income", Description = "Miscellaneous income sources", Color = "#A9DFBF", Icon = "fas fa-plus-circle" }
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            // Get the first user (or create a demo user)
            var user = await userManager.FindByEmailAsync("demo@smartexpensetracker.com");
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = "demo@smartexpensetracker.com",
                    Email = "demo@smartexpensetracker.com",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "Demo123!");
            }

            var userId = user.Id;

            // Create sample expenses for the last 6 months
            var expenses = new List<Expense>();
            var random = new Random();
            var startDate = DateTime.Now.AddMonths(-6);

            for (int month = 0; month < 6; month++)
            {
                var monthStart = startDate.AddMonths(month);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                // Generate 15-25 expenses per month
                var expenseCount = random.Next(15, 26);
                for (int i = 0; i < expenseCount; i++)
                {
                    var expenseDate = monthStart.AddDays(random.Next(0, (monthEnd - monthStart).Days + 1));
                    var categoryIndex = random.Next(0, 8); // First 8 categories are expenses
                    var category = categories[categoryIndex];

                    var amount = category.Name switch
                    {
                        "Food & Dining" => random.Next(10, 80),
                        "Transportation" => random.Next(20, 150),
                        "Shopping" => random.Next(25, 200),
                        "Entertainment" => random.Next(15, 100),
                        "Bills & Utilities" => random.Next(50, 300),
                        "Healthcare" => random.Next(30, 250),
                        "Education" => random.Next(100, 500),
                        "Travel" => random.Next(200, 1000),
                        _ => random.Next(20, 100)
                    };

                    var descriptions = category.Name switch
                    {
                        "Food & Dining" => new[] { "Restaurant dinner", "Grocery shopping", "Coffee shop", "Fast food lunch", "Takeout order" },
                        "Transportation" => new[] { "Gas station", "Public transport", "Taxi ride", "Car maintenance", "Parking fee" },
                        "Shopping" => new[] { "Clothing store", "Electronics", "Home supplies", "Online purchase", "Department store" },
                        "Entertainment" => new[] { "Movie tickets", "Concert", "Streaming service", "Gaming", "Sports event" },
                        "Bills & Utilities" => new[] { "Electricity bill", "Internet bill", "Phone bill", "Water bill", "Insurance" },
                        "Healthcare" => new[] { "Doctor visit", "Pharmacy", "Dental checkup", "Medical test", "Health insurance" },
                        "Education" => new[] { "Course fee", "Books", "Online learning", "Workshop", "Certification" },
                        "Travel" => new[] { "Flight ticket", "Hotel booking", "Vacation expense", "Business trip", "Weekend getaway" },
                        _ => new[] { "Miscellaneous expense", "Other purchase", "General expense" }
                    };

                    expenses.Add(new Expense
                    {
                        Amount = amount,
                        Description = descriptions[random.Next(descriptions.Length)],
                        Date = expenseDate,
                        CategoryId = category.Id,
                        UserId = userId
                    });
                }
            }

            context.Expenses.AddRange(expenses);

            // Create sample income for the last 6 months
            var incomes = new List<Income>();
            for (int month = 0; month < 6; month++)
            {
                var monthStart = startDate.AddMonths(month);
                
                // Monthly salary
                incomes.Add(new Income
                {
                    Amount = random.Next(4000, 6000),
                    Description = "Monthly Salary",
                    Date = monthStart.AddDays(random.Next(1, 5)),
                    CategoryId = categories.First(c => c.Name == "Salary").Id,
                    UserId = userId
                });

                // Occasional freelance work
                if (random.Next(1, 4) == 1) // 33% chance
                {
                    incomes.Add(new Income
                    {
                        Amount = random.Next(500, 1500),
                        Description = "Freelance Project",
                        Date = monthStart.AddDays(random.Next(10, 25)),
                        CategoryId = categories.First(c => c.Name == "Freelance").Id,
                        UserId = userId
                    });
                }

                // Investment returns
                if (random.Next(1, 3) == 1) // 50% chance
                {
                    incomes.Add(new Income
                    {
                        Amount = random.Next(100, 500),
                        Description = "Investment Returns",
                        Date = monthStart.AddDays(random.Next(15, 28)),
                        CategoryId = categories.First(c => c.Name == "Investment").Id,
                        UserId = userId
                    });
                }
            }

            context.Incomes.AddRange(incomes);

            // Create sample budgets
            var budgets = new List<Budget>
            {
                new Budget
                {
                    Name = "Monthly Food Budget",
                    Amount = 800,
                    StartDate = DateTime.Now.AddDays(-DateTime.Now.Day + 1), // First day of current month
                    EndDate = DateTime.Now.AddDays(-DateTime.Now.Day + 1).AddMonths(1).AddDays(-1), // Last day of current month
                    CategoryId = categories.First(c => c.Name == "Food & Dining").Id,
                    IsActive = true,
                    Description = "Monthly budget for food and dining expenses",
                    UserId = userId
                },
                new Budget
                {
                    Name = "Transportation Budget",
                    Amount = 400,
                    StartDate = DateTime.Now.AddDays(-DateTime.Now.Day + 1),
                    EndDate = DateTime.Now.AddDays(-DateTime.Now.Day + 1).AddMonths(1).AddDays(-1),
                    CategoryId = categories.First(c => c.Name == "Transportation").Id,
                    IsActive = true,
                    Description = "Monthly budget for transportation costs",
                    UserId = userId
                },
                new Budget
                {
                    Name = "Entertainment Budget",
                    Amount = 300,
                    StartDate = DateTime.Now.AddDays(-DateTime.Now.Day + 1),
                    EndDate = DateTime.Now.AddDays(-DateTime.Now.Day + 1).AddMonths(1).AddDays(-1),
                    CategoryId = categories.First(c => c.Name == "Entertainment").Id,
                    IsActive = true,
                    Description = "Monthly budget for entertainment and leisure",
                    UserId = userId
                },
                new Budget
                {
                    Name = "Shopping Budget",
                    Amount = 600,
                    StartDate = DateTime.Now.AddDays(-DateTime.Now.Day + 1),
                    EndDate = DateTime.Now.AddDays(-DateTime.Now.Day + 1).AddMonths(1).AddDays(-1),
                    CategoryId = categories.First(c => c.Name == "Shopping").Id,
                    IsActive = true,
                    Description = "Monthly budget for shopping and purchases",
                    UserId = userId
                }
            };

            context.Budgets.AddRange(budgets);
            await context.SaveChangesAsync();
        }
    }
}