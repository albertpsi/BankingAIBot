using BankingAIBot.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BankingAIBot.API.Data;

public static class DbSeeder
{
    public static void Seed(BankingDbContext context)
    {
        if (!context.AccountTypes.Any())
        {
            context.AccountTypes.Add(new AccountTypeData { Name = "Savings" });
            context.AccountTypes.Add(new AccountTypeData { Name = "Current" });
            context.SaveChanges();
        }

        NormalizeLegacyData(context);

        if (context.Users.Any()) return; // Database has been seeded

        // Create mock user
        var user = new User
        {
            Name = "John Doe",
            Email = "john@example.com",
            Role = "Customer",
            ConsentToAiProcessing = true,
            ConsentToAnalytics = true
        };
        var passwordHasher = new PasswordHasher<User>();
        user.PasswordHash = passwordHasher.HashPassword(user, "password123");
        context.Users.Add(user);
        context.SaveChanges();

        // Create accounts
        var current = new Account
        {
            UserId = user.UserId,
            AccountType = "Current",
            DisplayName = "Current",
            ExternalAccountId = "acct_current_001",
            AccountStatus = "Active",
            Balance = 2500.50m,
            AvailableBalance = 2400.50m,
            Currency = "INR"
        };
        var savings = new Account
        {
            UserId = user.UserId,
            AccountType = "Savings",
            DisplayName = "Rainy Day Savings",
            ExternalAccountId = "acct_savings_001",
            AccountStatus = "Active",
            Balance = 15000.00m,
            AvailableBalance = 15000.00m,
            Currency = "INR"
        };
        context.Accounts.Add(current);
        context.Accounts.Add(savings);
        context.SaveChanges();

        // Create realistic transactions
        var transactions = new List<Transaction>
        {
            new() { AccountId = current.AccountId, Amount = -15.50m, TransactionType = "Debit", Category = "Dining", MerchantName = "Starbucks", Timestamp = DateTime.UtcNow.AddDays(-2) },
            new() { AccountId = current.AccountId, Amount = -120.00m, TransactionType = "Debit", Category = "Groceries", MerchantName = "Whole Foods", Timestamp = DateTime.UtcNow.AddDays(-3), Description = "Weekly grocery run" },
            new() { AccountId = current.AccountId, Amount = -1500.00m, TransactionType = "Debit", Category = "Housing", MerchantName = "Skyline Apartments", Timestamp = DateTime.UtcNow.AddDays(-15), Description = "Monthly rent payment" },
            new() { AccountId = current.AccountId, Amount = 4000.00m, TransactionType = "Credit", Category = "Income", MerchantName = "Acme Corp Payroll", Timestamp = DateTime.UtcNow.AddDays(-16), Description = "Salary deposit" },
            new() { AccountId = current.AccountId, Amount = -50.00m, TransactionType = "Debit", Category = "Entertainment", MerchantName = "Netflix", Timestamp = DateTime.UtcNow.AddDays(-20), Description = "Streaming subscription" },
            new() { AccountId = current.AccountId, Amount = -45.00m, TransactionType = "Debit", Category = "Transportation", MerchantName = "Uber", Timestamp = DateTime.UtcNow.AddDays(-21), Description = "Airport ride" },
            new() { AccountId = current.AccountId, Amount = -220.00m, TransactionType = "Debit", Category = "Shopping", MerchantName = "Amazon", Timestamp = DateTime.UtcNow.AddDays(-6), Description = "Household purchases" },
            new() { AccountId = current.AccountId, Amount = -89.00m, TransactionType = "Debit", Category = "Dining", MerchantName = "Local Bistro", Timestamp = DateTime.UtcNow.AddDays(-5), Description = "Dinner out" },
            new() { AccountId = savings.AccountId, Amount = 500.00m, TransactionType = "Credit", Category = "Transfer", MerchantName = "Current Account", Timestamp = DateTime.UtcNow.AddDays(-16), Description = "Automated transfer" },
        };
        
        context.Transactions.AddRange(transactions);
        context.SaveChanges();
    }

    private static void NormalizeLegacyData(BankingDbContext context)
    {
        var updated = false;

        foreach (var account in context.Accounts.ToList())
        {
            if (account.Currency == "USD")
            {
                account.Currency = "INR";
                updated = true;
            }

            if (string.Equals(account.AccountType, "Checking", StringComparison.OrdinalIgnoreCase))
            {
                account.AccountType = "Current";
                updated = true;
            }

            if (string.Equals(account.DisplayName, "Primary Current", StringComparison.OrdinalIgnoreCase))
            {
                account.DisplayName = "Current";
                updated = true;
            }
        }

        foreach (var transaction in context.Transactions.ToList())
        {
            if (string.Equals(transaction.MerchantName, "Checking Account", StringComparison.OrdinalIgnoreCase))
            {
                transaction.MerchantName = "Current Account";
                updated = true;
            }
        }

        foreach (var accountType in context.AccountTypes.ToList())
        {
            if (string.Equals(accountType.Name, "Checking", StringComparison.OrdinalIgnoreCase))
            {
                accountType.Name = "Current";
                updated = true;
            }
        }

        if (updated)
        {
            context.SaveChanges();
        }
    }
}
