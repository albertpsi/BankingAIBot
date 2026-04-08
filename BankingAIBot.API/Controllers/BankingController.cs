using System.Security.Claims;
using BankingAIBot.API.Contracts;
using BankingAIBot.API.Data;
using BankingAIBot.API.Models;
using BankingAIBot.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingAIBot.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BankingController : ApiControllerBase
{
    private readonly IBankingInsightsService _insightsService;
    private readonly BankingDbContext _context;
    private readonly ILogger<BankingController> _logger;

    public BankingController(
        IBankingInsightsService insightsService,
        BankingDbContext context,
        ILogger<BankingController> logger)
    {
        _insightsService = insightsService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("account-types")]
    public async Task<ActionResult<IReadOnlyList<AccountTypeDto>>> GetAccountTypes(CancellationToken cancellationToken = default)
    {
        try
        {
            var types = await _context.AccountTypes.Select(a => new AccountTypeDto(a.Id, a.Name)).ToListAsync(cancellationToken);
            return Ok(types);
        }
        catch (Exception ex)
        {
            return HandleException<IReadOnlyList<AccountTypeDto>>(ex, _logger);
        }
    }

    [HttpGet("overview")]
    public async Task<ActionResult<BankingSnapshotDto>> GetOverview([FromQuery] int lookbackDays = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = await _insightsService.BuildSnapshotAsync(GetUserId(), lookbackDays, cancellationToken);
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            return HandleException<BankingSnapshotDto>(ex, _logger);
        }
    }

    [HttpGet("accounts")]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts(CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _insightsService.GetAccountsAsync(GetUserId(), cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException<IReadOnlyList<AccountDto>>(ex, _logger);
        }
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetTransactions([FromQuery] int lookbackDays = 30, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _insightsService.GetRecentTransactionsAsync(GetUserId(), lookbackDays, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException<IReadOnlyList<TransactionDto>>(ex, _logger);
        }
    }

    [HttpGet("spending-summary")]
    public async Task<ActionResult<IReadOnlyList<CategorySpendDto>>> GetSpendingSummary(
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _insightsService.GetMonthlyCategorySpendAsync(GetUserId(), year, month, cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException<IReadOnlyList<CategorySpendDto>>(ex, _logger);
        }
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<IReadOnlyList<SavingsSuggestionDto>>> GetSuggestions(CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _insightsService.GetSavingsSuggestionsAsync(GetUserId(), cancellationToken));
        }
        catch (Exception ex)
        {
            return HandleException<IReadOnlyList<SavingsSuggestionDto>>(ex, _logger);
        }
    }

    [HttpPost("accounts/{accountId:int}/deposit")]
    public async Task<ActionResult<AccountMutationResponse>> Deposit(
        [FromRoute] int accountId,
        [FromBody] MoneyTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? dbTransaction = null;

        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest("Amount must be greater than zero.");
            }

            var account = await LoadAccountAsync(accountId, cancellationToken);
            if (account is null)
            {
                return NotFound();
            }

            dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            account.Balance += request.Amount;
            account.AvailableBalance += request.Amount;
            account.UpdatedAt = DateTime.UtcNow;

            var transaction = new Transaction
            {
                AccountId = account.AccountId,
                ExternalTransactionId = $"dep_{Guid.NewGuid():N}",
                Amount = request.Amount,
                TransactionType = "Credit",
                Category = "Transfer",
                MerchantName = string.IsNullOrWhiteSpace(request.MerchantName) ? "Cash Deposit" : request.MerchantName.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? "Manual deposit" : request.Description.Trim(),
                Timestamp = DateTime.UtcNow,
                PostedAt = DateTime.UtcNow,
                IsPending = false,
                BalanceAfter = account.Balance
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            return Ok(new AccountMutationResponse(MapAccountDto(account), MapTransactionDto(transaction, account.DisplayName)));
        }
        catch (Exception ex)
        {
            if (dbTransaction is not null)
            {
                await dbTransaction.RollbackAsync(cancellationToken);
            }

            return HandleException<AccountMutationResponse>(ex, _logger);
        }
    }

    [HttpPost("accounts/{accountId:int}/transactions")]
    public async Task<ActionResult<AccountMutationResponse>> CreateTransaction(
        [FromRoute] int accountId,
        [FromBody] NewTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? dbTransaction = null;

        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest("Amount must be greater than zero.");
            }

            var normalizedType = request.TransactionType.Trim().ToLowerInvariant();
            if (normalizedType is not ("debit" or "credit"))
            {
                return BadRequest("TransactionType must be either Debit or Credit.");
            }

            if (string.IsNullOrWhiteSpace(request.Category) || string.IsNullOrWhiteSpace(request.MerchantName) || string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest("Category, merchant name, and description are required.");
            }

            var account = await LoadAccountAsync(accountId, cancellationToken);
            if (account is null)
            {
                return NotFound();
            }

            dbTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            var signedAmount = normalizedType == "debit" ? -Math.Abs(request.Amount) : Math.Abs(request.Amount);
            if (signedAmount < 0 && account.AvailableBalance < Math.Abs(signedAmount))
            {
                return BadRequest("Insufficient available balance for this debit.");
            }

            account.Balance += signedAmount;
            account.AvailableBalance += signedAmount;
            account.UpdatedAt = DateTime.UtcNow;

            var transaction = new Transaction
            {
                AccountId = account.AccountId,
                ExternalTransactionId = $"{normalizedType}_{Guid.NewGuid():N}",
                Amount = signedAmount,
                TransactionType = char.ToUpperInvariant(normalizedType[0]) + normalizedType[1..],
                Category = request.Category.Trim(),
                MerchantName = request.MerchantName.Trim(),
                Description = request.Description.Trim(),
                Timestamp = DateTime.UtcNow,
                PostedAt = request.IsPending ? null : DateTime.UtcNow,
                IsPending = request.IsPending,
                BalanceAfter = account.Balance
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            return Ok(new AccountMutationResponse(MapAccountDto(account), MapTransactionDto(transaction, account.DisplayName)));
        }
        catch (Exception ex)
        {
            if (dbTransaction is not null)
            {
                await dbTransaction.RollbackAsync(cancellationToken);
            }

            return HandleException<AccountMutationResponse>(ex, _logger);
        }
    }

    private int GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("User identity is missing.");
    }

    private async Task<Account?> LoadAccountAsync(int accountId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        return await _context.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == accountId && a.UserId == userId, cancellationToken);
    }

    private static AccountDto MapAccountDto(Account account)
        => new(
            account.AccountId,
            account.AccountType,
            account.DisplayName,
            account.AccountStatus,
            account.Balance,
            account.AvailableBalance,
            account.Currency,
            account.IsActive);

    private static TransactionDto MapTransactionDto(Transaction transaction, string accountDisplayName)
        => new(
            transaction.TransactionId,
            transaction.AccountId,
            accountDisplayName,
            transaction.Amount,
            transaction.TransactionType,
            transaction.Category,
            transaction.Description,
            transaction.Timestamp,
            transaction.MerchantName,
            transaction.IsPending,
            transaction.BalanceAfter);
}
