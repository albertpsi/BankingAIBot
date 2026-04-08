using BankingAIBot.API.Contracts;
using BankingAIBot.API.Data;
using Microsoft.EntityFrameworkCore;

namespace BankingAIBot.API.Services;

public interface IBankingToolDataService
{
    Task<AccountInfoToolResultDto> GetAccountInfoAsync(int userId, CancellationToken cancellationToken = default);
    Task<TransactionsToolResultDto> GetTransactionsAsync(int userId, string type, int lookbackDays, CancellationToken cancellationToken = default);
    Task<TransactionsAndAccountInfoToolResultDto> GetTransactionsAndAccountInfoAsync(int userId, string type, int lookbackDays, CancellationToken cancellationToken = default);
    Task<TransactionsToolResultDto> GetTransactionsForDateAsync(int userId, string type, DateTime? dateUtc, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
    Task<TransactionsAndAccountInfoToolResultDto> GetTransactionsAndAccountInfoForDateAsync(int userId, string type, DateTime? dateUtc, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default);
}

public sealed class BankingToolDataService : IBankingToolDataService
{
    private readonly IDbContextFactory<BankingDbContext> _contextFactory;
    private readonly ILogger<BankingToolDataService> _logger;

    public BankingToolDataService(IDbContextFactory<BankingDbContext> contextFactory, ILogger<BankingToolDataService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task<AccountInfoToolResultDto> GetAccountInfoAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var accounts = await LoadAccountsAsync(context, userId, cancellationToken);

        return new AccountInfoToolResultDto(
            accounts.Sum(a => a.Balance),
            accounts.Sum(a => a.AvailableBalance),
            accounts);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load tool account info for user {UserId}.", userId);
            throw;
        }
    }

    public async Task<TransactionsToolResultDto> GetTransactionsAsync(int userId, string type, int lookbackDays, CancellationToken cancellationToken = default)
    {
        try
        {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedType = NormalizeTransactionType(type);
        var normalizedDays = NormalizeLookbackDays(lookbackDays, 90);
        var since = DateTime.UtcNow.AddDays(-normalizedDays);
        var transactions = await LoadTransactionsAsync(context, userId, normalizedType, since, cancellationToken);

        return new TransactionsToolResultDto(
            normalizedType,
            normalizedDays,
            transactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
            transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount)),
            transactions.Count,
            transactions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load tool transactions for user {UserId}.", userId);
            throw;
        }
    }

    public async Task<TransactionsAndAccountInfoToolResultDto> GetTransactionsAndAccountInfoAsync(int userId, string type, int lookbackDays, CancellationToken cancellationToken = default)
    {
        try
        {
        var accountTask = GetAccountInfoAsync(userId, cancellationToken);
        var transactionTask = GetTransactionsAsync(userId, type, lookbackDays, cancellationToken);

        await Task.WhenAll(accountTask, transactionTask);

        var accountInfo = await accountTask;
        var transactionInfo = await transactionTask;

        return new TransactionsAndAccountInfoToolResultDto(
            transactionInfo.AppliedType,
            transactionInfo.Days,
            accountInfo.TotalBalance,
            accountInfo.TotalAvailableBalance,
            transactionInfo.TotalCreditAmount,
            transactionInfo.TotalDebitAmount,
            transactionInfo.TransactionCount,
            accountInfo.Accounts,
            transactionInfo.Transactions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load combined tool data for user {UserId}.", userId);
            throw;
        }
    }

    public async Task<TransactionsToolResultDto> GetTransactionsForDateAsync(int userId, string type, DateTime? dateUtc, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        try
        {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Determine range. If `dateUtc` provided, treat as that calendar day (00:00 UTC inclusive to next day exclusive).
        DateTime startUtc;
        DateTime endUtc;

        if (dateUtc.HasValue)
        {
            startUtc = DateTime.SpecifyKind(dateUtc.Value.Date, DateTimeKind.Utc);
            endUtc = startUtc.AddDays(1);
        }
        else if (fromUtc.HasValue || toUtc.HasValue)
        {
            startUtc = fromUtc?.Date ?? DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            // If `toUtc` provided, include that day by using next-day exclusive bound
            endUtc = (toUtc?.Date ?? startUtc).AddDays(1);
        }
        else
        {
            // No dates provided — fall back to last 1 day
            startUtc = DateTime.UtcNow.Date;
            endUtc = startUtc.AddDays(1);
        }

        var normalizedType = NormalizeTransactionType(type);
        var transactions = await LoadTransactionsRangeAsync(context, userId, normalizedType, startUtc, endUtc, cancellationToken);

        var days = Math.Max(1, (int)Math.Ceiling((endUtc - startUtc).TotalDays));

        return new TransactionsToolResultDto(
            normalizedType,
            days,
            transactions.Where(t => t.Amount > 0).Sum(t => t.Amount),
            transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount)),
            transactions.Count,
            transactions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load tool transactions by date for user {UserId}.", userId);
            throw;
        }
    }

    public async Task<TransactionsAndAccountInfoToolResultDto> GetTransactionsAndAccountInfoForDateAsync(int userId, string type, DateTime? dateUtc, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
    {
        try
        {
            var accountTask = GetAccountInfoAsync(userId, cancellationToken);
            var transactionTask = GetTransactionsForDateAsync(userId, type, dateUtc, fromUtc, toUtc, cancellationToken);

            await Task.WhenAll(accountTask, transactionTask);

            var accountInfo = await accountTask;
            var transactionInfo = await transactionTask;

            return new TransactionsAndAccountInfoToolResultDto(
                transactionInfo.AppliedType,
                transactionInfo.Days,
                accountInfo.TotalBalance,
                accountInfo.TotalAvailableBalance,
                transactionInfo.TotalCreditAmount,
                transactionInfo.TotalDebitAmount,
                transactionInfo.TransactionCount,
                accountInfo.Accounts,
                transactionInfo.Transactions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to load combined date-range tool data for user {UserId}.", userId);
            throw;
        }
    }

    private static async Task<IReadOnlyList<TransactionDto>> LoadTransactionsRangeAsync(
        BankingDbContext context,
        int userId,
        string normalizedType,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var query = context.Transactions
            .AsNoTracking()
            .Where(t => t.Account.UserId == userId && t.Account.IsActive && t.Timestamp >= fromUtc && t.Timestamp < toUtc);

        var storedType = normalizedType switch
        {
            "credit" => "Credit",
            "debit" => "Debit",
            _ => null
        };

        if (storedType is not null)
        {
            query = query.Where(t => t.TransactionType == storedType);
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

    private static async Task<IReadOnlyList<AccountDto>> LoadAccountsAsync(BankingDbContext context, int userId, CancellationToken cancellationToken)
    {
        return await context.Accounts
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

    private static async Task<IReadOnlyList<TransactionDto>> LoadTransactionsAsync(
        BankingDbContext context,
        int userId,
        string normalizedType,
        DateTime since,
        CancellationToken cancellationToken)
    {
        var query = context.Transactions
            .AsNoTracking()
            .Where(t => t.Account.UserId == userId && t.Account.IsActive && t.Timestamp >= since);

        var storedType = normalizedType switch
        {
            "credit" => "Credit",
            "debit" => "Debit",
            _ => null
        };

        if (storedType is not null)
        {
            query = query.Where(t => t.TransactionType == storedType);
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

    private static int NormalizeLookbackDays(int lookbackDays, int fallback)
    {
        var days = lookbackDays <= 0 ? fallback : lookbackDays;
        return Math.Clamp(days, 1, 365);
    }

    private static string NormalizeTransactionType(string? type)
    {
        var normalized = type?.Trim().ToLowerInvariant();
        return normalized is "credit" or "debit" ? normalized : "all";
    }
}
