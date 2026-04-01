using BankingAIBot.API.Contracts;
using BankingAIBot.API.Data;
using BankingAIBot.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingAIBot.API.Services;

public interface IBankingInsightsService
{
    Task<BankingSnapshotDto> BuildSnapshotAsync(int userId, int lookbackDays, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TransactionDto>> GetRecentTransactionsAsync(int userId, int lookbackDays, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CategorySpendDto>> GetMonthlyCategorySpendAsync(int userId, int year, int month, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SavingsSuggestionDto>> GetSavingsSuggestionsAsync(int userId, CancellationToken cancellationToken = default);
}

public sealed class BankingInsightsService : IBankingInsightsService
{
    private readonly BankingDbContext _context;

    public BankingInsightsService(BankingDbContext context)
    {
        _context = context;
    }

    public async Task<BankingSnapshotDto> BuildSnapshotAsync(int userId, int lookbackDays, CancellationToken cancellationToken = default)
    {
        lookbackDays = Math.Max(7, lookbackDays);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);
        var recentCutoff = now.AddDays(-lookbackDays);

        var accounts = await LoadAccountDtosAsync(userId, cancellationToken);
        var accountIds = accounts.Select(a => a.AccountId).ToArray();

        if (accountIds.Length == 0)
        {
            return new BankingSnapshotDto(
                0m,
                0m,
                0m,
                0m,
                0m,
                accounts,
                Array.Empty<TransactionDto>(),
                Array.Empty<CategorySpendDto>(),
                Array.Empty<SavingsSuggestionDto>());
        }

        var recentTransactions = await LoadRecentTransactionsAsync(accountIds, recentCutoff, cancellationToken);
        var monthlyTransactions = await LoadRecentTransactionsAsync(accountIds, monthStart, cancellationToken, nextMonthStart);
        var categorySpend = BuildCategorySpend(monthlyTransactions);
        var suggestions = BuildSuggestions(monthlyTransactions, categorySpend, now);

        await PersistDerivedDataAsync(userId, now, categorySpend, suggestions, cancellationToken);

        var monthlyIncome = monthlyTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
        var monthlySpend = monthlyTransactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
        var totalBalance = accounts.Sum(a => a.Balance);
        var totalAvailableBalance = accounts.Sum(a => a.AvailableBalance);

        return new BankingSnapshotDto(
            totalBalance,
            totalAvailableBalance,
            monthlyIncome,
            monthlySpend,
            monthlyIncome - monthlySpend,
            accounts,
            recentTransactions,
            categorySpend,
            suggestions);
    }

    public Task<IReadOnlyList<AccountDto>> GetAccountsAsync(int userId, CancellationToken cancellationToken = default)
        => LoadAccountDtosAsync(userId, cancellationToken);

    public async Task<IReadOnlyList<TransactionDto>> GetRecentTransactionsAsync(int userId, int lookbackDays, CancellationToken cancellationToken = default)
    {
        lookbackDays = Math.Max(7, lookbackDays);
        var accountIds = await GetUserAccountIdsAsync(userId, cancellationToken);
        if (accountIds.Length == 0)
        {
            return Array.Empty<TransactionDto>();
        }

        var recentCutoff = DateTime.UtcNow.AddDays(-lookbackDays);
        var transactions = await LoadRecentTransactionsAsync(accountIds, recentCutoff, cancellationToken);
        return transactions;
    }

    public async Task<IReadOnlyList<CategorySpendDto>> GetMonthlyCategorySpendAsync(int userId, int year, int month, CancellationToken cancellationToken = default)
    {
        var accountIds = await GetUserAccountIdsAsync(userId, cancellationToken);
        if (accountIds.Length == 0)
        {
            return Array.Empty<CategorySpendDto>();
        }

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);
        var transactions = await LoadRecentTransactionsAsync(accountIds, start, cancellationToken, end);
        return BuildCategorySpend(transactions);
    }

    public async Task<IReadOnlyList<SavingsSuggestionDto>> GetSavingsSuggestionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var suggestions = await _context.SavingsSuggestions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SavingsSuggestionDto(
                s.SavingsSuggestionId,
                s.Title,
                s.Reason,
                s.EstimatedMonthlySavings,
                s.Priority,
                s.Status,
                s.CreatedAt))
            .ToListAsync(cancellationToken);

        return suggestions;
    }

    private async Task<IReadOnlyList<AccountDto>> LoadAccountDtosAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsActive)
            .OrderBy(a => a.AccountType)
            .ThenBy(a => a.DisplayName)
            .Select(a => new AccountDto(
                a.AccountId,
                a.AccountType,
                a.DisplayName,
                a.AccountStatus,
                a.Balance,
                a.AvailableBalance,
                a.Currency,
                a.IsActive))
            .ToListAsync(cancellationToken);
    }

    private async Task<int[]> GetUserAccountIdsAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.IsActive)
            .Select(a => a.AccountId)
            .ToArrayAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<TransactionDto>> LoadRecentTransactionsAsync(
        int[] accountIds,
        DateTime since,
        CancellationToken cancellationToken,
        DateTime? until = null)
    {
        var query = _context.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId) && t.Timestamp >= since);

        if (until.HasValue)
        {
            query = query.Where(t => t.Timestamp < until.Value);
        }

        return await query
            .OrderByDescending(t => t.Timestamp)
            .Select(t => new TransactionDto(
                t.TransactionId,
                t.AccountId,
                t.Account.DisplayName,
                t.Amount,
                t.TransactionType,
                t.Category,
                t.Description,
                t.Timestamp,
                t.MerchantName,
                t.IsPending,
                t.BalanceAfter))
            .ToListAsync(cancellationToken);
    }


    private static IReadOnlyList<CategorySpendDto> BuildCategorySpend(IReadOnlyList<TransactionDto> transactions)
    {
        return transactions
            .Where(t => t.Amount < 0)
            .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "Uncategorized" : t.Category)
            .Select(group => new CategorySpendDto(
                group.Key,
                group.Sum(t => Math.Abs(t.Amount)),
                group.Count()))
            .OrderByDescending(c => c.TotalAmount)
            .ToList();
    }

    private static IReadOnlyList<SavingsSuggestionDto> BuildSuggestions(
        IReadOnlyList<TransactionDto> transactions,
        IReadOnlyList<CategorySpendDto> categorySpend,
        DateTime now)
    {
        var suggestions = new List<SavingsSuggestionDto>();
        var totalSpend = categorySpend.Sum(c => c.TotalAmount);
        var dining = categorySpend.FirstOrDefault(c => string.Equals(c.Category, "Dining", StringComparison.OrdinalIgnoreCase));
        var shopping = categorySpend.FirstOrDefault(c => string.Equals(c.Category, "Shopping", StringComparison.OrdinalIgnoreCase));
        var entertainment = categorySpend.FirstOrDefault(c => string.Equals(c.Category, "Entertainment", StringComparison.OrdinalIgnoreCase));
        var transportation = categorySpend.FirstOrDefault(c => string.Equals(c.Category, "Transportation", StringComparison.OrdinalIgnoreCase));

        if (dining is not null && dining.TotalAmount >= 100m)
        {
            suggestions.Add(new SavingsSuggestionDto(
                0,
                "Trim dining spend",
                $"Dining spend is {dining.TotalAmount:C0} this month. Cutting back by 15% could free up about {(dining.TotalAmount * 0.15m):C0}.",
                Math.Round(dining.TotalAmount * 0.15m, 2),
                "Medium",
                "Open",
                now));
        }

        if (shopping is not null && shopping.TotalAmount >= 150m)
        {
            suggestions.Add(new SavingsSuggestionDto(
                0,
                "Delay non-essential shopping",
                $"Shopping spend is {shopping.TotalAmount:C0} this month. Waiting 48 hours before purchases may save about {(shopping.TotalAmount * 0.10m):C0}.",
                Math.Round(shopping.TotalAmount * 0.10m, 2),
                "Medium",
                "Open",
                now));
        }

        if (entertainment is not null && entertainment.TotalAmount >= 40m)
        {
            suggestions.Add(new SavingsSuggestionDto(
                0,
                "Review subscriptions",
                $"Entertainment spend is {entertainment.TotalAmount:C0} this month. Reviewing streaming or subscription services could save around {(entertainment.TotalAmount * 0.25m):C0}.",
                Math.Round(entertainment.TotalAmount * 0.25m, 2),
                "Low",
                "Open",
                now));
        }

        if (transportation is not null && transportation.TotalAmount >= 50m)
        {
            suggestions.Add(new SavingsSuggestionDto(
                0,
                "Reduce ride-hailing",
                $"Transportation spend is {transportation.TotalAmount:C0} this month. Combining errands or using transit could save about {(transportation.TotalAmount * 0.20m):C0}.",
                Math.Round(transportation.TotalAmount * 0.20m, 2),
                "Low",
                "Open",
                now));
        }

        if (totalSpend >= 1000m && !suggestions.Any(s => s.Title.Contains("budget", StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add(new SavingsSuggestionDto(
                0,
                "Create a monthly budget target",
                $"Monthly spend is {totalSpend:C0}. Setting category budgets could keep spending predictable and identify overshoot early.",
                Math.Round(totalSpend * 0.05m, 2),
                "High",
                "Open",
                now));
        }

        var duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return suggestions
            .Where(s => duplicateKeys.Add(s.Title))
            .ToList();
    }

    private async Task PersistDerivedDataAsync(
        int userId,
        DateTime now,
        IReadOnlyList<CategorySpendDto> categorySpend,
        IReadOnlyList<SavingsSuggestionDto> suggestions,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = monthStart.AddMonths(1);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await _context.SpendingSummaries
                .Where(s => s.UserId == userId && s.Year == now.Year && s.Month == now.Month)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.SavingsSuggestions
                .Where(s => s.UserId == userId && s.CreatedAt >= monthStart && s.CreatedAt < nextMonthStart && s.Status == "Open")
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var summary in categorySpend)
            {
                _context.SpendingSummaries.Add(new SpendingSummary
                {
                    UserId = userId,
                    Year = now.Year,
                    Month = now.Month,
                    Category = summary.Category,
                    TotalAmount = summary.TotalAmount,
                    TransactionCount = summary.TransactionCount,
                    CalculatedAt = now
                });
            }

            foreach (var suggestion in suggestions)
            {
                _context.SavingsSuggestions.Add(new SavingsSuggestion
                {
                    UserId = userId,
                    Title = suggestion.Title,
                    Reason = suggestion.Reason,
                    EstimatedMonthlySavings = suggestion.EstimatedMonthlySavings,
                    Priority = suggestion.Priority,
                    Status = suggestion.Status,
                    CreatedAt = now
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }
}
