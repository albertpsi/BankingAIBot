namespace BankingAIBot.API.Contracts;

public record AccountDto(
    int AccountId,
    string AccountType,
    string DisplayName,
    string AccountStatus,
    decimal Balance,
    decimal AvailableBalance,
    string Currency,
    bool IsActive);

public record AccountTypeDto(
    int Id,
    string Name);

public record TransactionDto(
    int TransactionId,
    int AccountId,
    string AccountDisplayName,
    decimal Amount,
    string TransactionType,
    string Category,
    string Description,
    DateTime Timestamp,
    string MerchantName,
    bool IsPending,
    decimal? BalanceAfter);

public record AccountInfoToolResultDto(
    decimal TotalBalance,
    decimal TotalAvailableBalance,
    IReadOnlyList<AccountDto> Accounts);

public record TransactionsToolResultDto(
    string AppliedType,
    int Days,
    int TransactionCount,
    IReadOnlyList<TransactionDto> Transactions);

public record TransactionsAndAccountInfoToolResultDto(
    string AppliedType,
    int Days,
    decimal TotalBalance,
    decimal TotalAvailableBalance,
    int TransactionCount,
    IReadOnlyList<AccountDto> Accounts,
    IReadOnlyList<TransactionDto> Transactions);

public record CategorySpendDto(
    string Category,
    decimal TotalAmount,
    int TransactionCount);

public record SavingsSuggestionDto(
    int SavingsSuggestionId,
    string Title,
    string Reason,
    decimal EstimatedMonthlySavings,
    string Priority,
    string Status,
    DateTime CreatedAt);

public record BankingSnapshotDto(
    decimal TotalBalance,
    decimal TotalAvailableBalance,
    decimal MonthlyIncome,
    decimal MonthlySpend,
    decimal NetCashFlow,
    IReadOnlyList<AccountDto> Accounts,
    IReadOnlyList<TransactionDto> RecentTransactions,
    IReadOnlyList<CategorySpendDto> CategorySpend,
    IReadOnlyList<SavingsSuggestionDto> SavingsSuggestions);

public record ChatRequest(
    string Message,
    int? SessionId = null,
    int LookbackDays = 30);

public record ChatResponse(
    int SessionId,
    string AssistantMessage,
    string ModelName,
    string PromptVersion,
    BankingSnapshotDto Snapshot,
    IReadOnlyList<SavingsSuggestionDto> SavingsSuggestions);

public record ChatSessionListDto(
    int SessionId,
    string TitleSummary,
    DateTime StartedAt,
    DateTime? LastMessageAt,
    string Status,
    string? ModelName);

public record ChatMessageDto(
    int MessageId,
    string Role,
    string Content,
    DateTime Timestamp,
    string? ToolName,
    string? ToolCallId,
    string? ModelName,
    string? PromptVersion);

public record ChatSessionDetailsDto(
    int SessionId,
    string TitleSummary,
    DateTime StartedAt,
    DateTime? LastMessageAt,
    string Status,
    string? ModelName,
    IReadOnlyList<ChatMessageDto> Messages);

public record MoneyTransferRequest(
    decimal Amount,
    string Description,
    string? MerchantName = null);

public record NewTransactionRequest(
    decimal Amount,
    string TransactionType,
    string Category,
    string MerchantName,
    string Description,
    bool IsPending = false);

public record AccountMutationResponse(
    AccountDto Account,
    TransactionDto Transaction);

public record SavedPromptDto(
    int SavedPromptId,
    string Title,
    string PromptText,
    int UsageCount,
    bool IsPinned,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record SavePromptRequest(
    string Title,
    string PromptText,
    bool IsPinned = false);
