namespace BankingAIBot.API.Models;

public class User
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public bool ConsentToAiProcessing { get; set; } = true;
    public bool ConsentToAnalytics { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public ICollection<ConsentRecord> ConsentRecords { get; set; } = new List<ConsentRecord>();
    public ICollection<ModelInvocationLog> ModelInvocationLogs { get; set; } = new List<ModelInvocationLog>();
    public ICollection<SpendingSummary> SpendingSummaries { get; set; } = new List<SpendingSummary>();
    public ICollection<SavingsSuggestion> SavingsSuggestions { get; set; } = new List<SavingsSuggestion>();
    public ICollection<SavedPrompt> SavedPrompts { get; set; } = new List<SavedPrompt>();
}

public class Account
{
    public int AccountId { get; set; }
    public int UserId { get; set; }
    public string AccountType { get; set; } = string.Empty; // e.g. Current, Savings, Credit
    public string DisplayName { get; set; } = string.Empty;
    public string ExternalAccountId { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = "Active";
    public decimal Balance { get; set; }
    public decimal AvailableBalance { get; set; }
    public string Currency { get; set; } = "INR";
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public class Transaction
{
    public int TransactionId { get; set; }
    public int AccountId { get; set; }
    public string ExternalTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty; // Debit/Credit
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime? PostedAt { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public bool IsPending { get; set; }
    public decimal? BalanceAfter { get; set; }

    public Account Account { get; set; } = null!;
}

public class ChatSession
{
    public int SessionId { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastMessageAt { get; set; }
    public string Status { get; set; } = "Active";
    public string? ModelName { get; set; }
    public string TitleSummary { get; set; } = string.Empty;
    public string? ContextSummary { get; set; }

    public User User { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public ICollection<ModelInvocationLog> ModelInvocationLogs { get; set; } = new List<ModelInvocationLog>();
}

public class ChatMessage
{
    public int MessageId { get; set; }
    public int SessionId { get; set; }
    public string Role { get; set; } = string.Empty; // User, Assistant, System, Tool
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ToolName { get; set; }
    public string? ToolCallId { get; set; }
    public string? ToolArgumentsJson { get; set; }
    public string? ToolResultJson { get; set; }
    public string? ModelName { get; set; }
    public string? PromptVersion { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }

    public ChatSession Session { get; set; } = null!;
}

public class SpendingSummary
{
    public int SpendingSummaryId { get; set; }
    public int UserId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

public class SavingsSuggestion
{
    public int SavingsSuggestionId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal EstimatedMonthlySavings { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public User User { get; set; } = null!;
}

public class ConsentRecord
{
    public int ConsentRecordId { get; set; }
    public int UserId { get; set; }
    public string ConsentType { get; set; } = string.Empty;
    public bool Granted { get; set; }
    public string Source { get; set; } = "System";
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}

public class ModelInvocationLog
{
    public int ModelInvocationLogId { get; set; }
    public int UserId { get; set; }
    public int? SessionId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string RequestJson { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ChatSession? Session { get; set; }
}

public class SavedPrompt
{
    public int SavedPromptId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

public class AccountTypeData
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
