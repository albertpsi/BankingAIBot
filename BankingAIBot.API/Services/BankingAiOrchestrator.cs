using System.Text.Json;
using System.Text.Json.Nodes;
using BankingAIBot.API.Contracts;
using BankingAIBot.API.Data;
using BankingAIBot.API.Models;
using BankingAIBot.API.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BankingAIBot.API.Services;

public interface IBankingAiOrchestrator
{
    Task<ChatResponse> RespondAsync(int userId, ChatRequest request, CancellationToken cancellationToken = default);
    Task<ChatSessionDetailsDto?> GetSessionAsync(int userId, int sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatSessionListDto>> ListSessionsAsync(int userId, CancellationToken cancellationToken = default);
}

public sealed class BankingAiOrchestrator : IBankingAiOrchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly BankingDbContext _context;
    private readonly IBankingInsightsService _insightsService;
    private readonly IBankingToolExecutor _toolExecutor;
    private readonly OpenAiChatClient _openAiClient;
    private readonly OpenAiOptions _options;

    public BankingAiOrchestrator(
        BankingDbContext context,
        IBankingInsightsService insightsService,
        IBankingToolExecutor toolExecutor,
        OpenAiChatClient openAiClient,
        IOptions<OpenAiOptions> options)
    {
        _context = context;
        _insightsService = insightsService;
        _toolExecutor = toolExecutor;
        _openAiClient = openAiClient;
        _options = options.Value;
    }

    public async Task<ChatResponse> RespondAsync(int userId, ChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message cannot be empty.", nameof(request));
        }

        var startedAt = DateTime.UtcNow;
        var session = await GetOrCreateSessionAsync(userId, request.SessionId, request.Message, cancellationToken);
        var userMessage = new ChatMessage
        {
            SessionId = session.SessionId,
            Role = "User",
            Content = request.Message,
            Timestamp = startedAt
        };

        _context.ChatMessages.Add(userMessage);
        session.LastMessageAt = startedAt;
        session.ModelName ??= _options.Model;

        var snapshot = await _insightsService.BuildSnapshotAsync(userId, request.LookbackDays, cancellationToken);
        var modelName = _options.IsConfigured ? _options.Model : "local-fallback";
        var promptVersion = _options.PromptVersion;

        string assistantMessage;
        OpenAiUsage? usage = null;
        string responseJson;

        if (_options.IsConfigured)
        {
            var toolDefinitions = BuildToolDefinitions();
            var messages = await BuildConversationAsync(session.SessionId, cancellationToken);
            var finalMessage = await ExecuteWithToolsAsync(userId, session.SessionId, messages, toolDefinitions, cancellationToken);
            assistantMessage = finalMessage.Message.Content?.Trim() ?? string.Empty;
            usage = finalMessage.Usage;
            responseJson = finalMessage.RawJson;
        }
        else
        {
            assistantMessage = BuildLocalResponse(snapshot);
            responseJson = JsonSerializer.Serialize(new
            {
                response = assistantMessage,
                snapshot
            }, JsonOptions);
        }

        _context.ChatMessages.Add(new ChatMessage
        {
            SessionId = session.SessionId,
            Role = "Assistant",
            Content = assistantMessage,
            Timestamp = DateTime.UtcNow,
            ModelName = modelName,
            PromptVersion = promptVersion,
            PromptTokens = usage?.PromptTokens,
            CompletionTokens = usage?.CompletionTokens,
            TotalTokens = usage?.TotalTokens
        });

        session.LastMessageAt = DateTime.UtcNow;
        session.ModelName = modelName;
        if (string.IsNullOrWhiteSpace(session.TitleSummary))
        {
            session.TitleSummary = BuildSessionTitle(request.Message);
        }

        _context.ModelInvocationLogs.Add(new ModelInvocationLog
        {
            UserId = userId,
            SessionId = session.SessionId,
            Endpoint = _options.IsConfigured ? _options.Endpoint : "local-fallback",
            ModelName = modelName,
            PromptVersion = promptVersion,
            RequestJson = JsonSerializer.Serialize(new
            {
                request.Message,
                request.SessionId,
                request.LookbackDays
            }, JsonOptions),
            ResponseJson = responseJson,
            Succeeded = true,
            LatencyMs = (int)Math.Max(0, (DateTime.UtcNow - startedAt).TotalMilliseconds),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new ChatResponse(
            session.SessionId,
            assistantMessage,
            modelName,
            promptVersion,
            snapshot,
            snapshot.SavingsSuggestions);
    }

    public async Task<ChatSessionDetailsDto?> GetSessionAsync(int userId, int sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.ChatSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionId == sessionId, cancellationToken);

        if (session is null)
        {
            return null;
        }

        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.SessionId == session.SessionId)
            .OrderBy(m => m.Timestamp)
            .Select(m => new ChatMessageDto(
                m.MessageId,
                m.Role,
                m.Content,
                m.Timestamp,
                m.ToolName,
                m.ToolCallId,
                m.ModelName,
                m.PromptVersion))
            .ToListAsync(cancellationToken);

        return new ChatSessionDetailsDto(
            session.SessionId,
            session.TitleSummary,
            session.StartedAt,
            session.LastMessageAt,
            session.Status,
            session.ModelName,
            messages);
    }

    public async Task<IReadOnlyList<ChatSessionListDto>> ListSessionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.ChatSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LastMessageAt ?? s.StartedAt)
            .Select(s => new ChatSessionListDto(
                s.SessionId,
                s.TitleSummary,
                s.StartedAt,
                s.LastMessageAt,
                s.Status,
                s.ModelName))
            .ToListAsync(cancellationToken);
    }

    private async Task<ChatSession> GetOrCreateSessionAsync(int userId, int? sessionId, string titleSeed, CancellationToken cancellationToken)
    {
        if (sessionId.HasValue)
        {
            var existing = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId.Value && s.UserId == userId, cancellationToken);

            if (existing is not null)
            {
                return existing;
            }
        }

        var session = new ChatSession
        {
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            TitleSummary = BuildSessionTitle(titleSeed),
            Status = "Active",
            ModelName = _options.IsConfigured ? _options.Model : "local-fallback",
            ContextSummary = "POC banking conversation"
        };

        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    private async Task<OpenAiCompletionResult> ExecuteWithToolsAsync(
        int userId,
        int sessionId,
        List<OpenAiMessage> messages,
        IReadOnlyList<OpenAiToolDefinition> toolDefinitions,
        CancellationToken cancellationToken)
    {
        var iterations = 0;
        while (true)
        {
            iterations++;
            if (iterations > 4)
            {
                throw new InvalidOperationException("The assistant exceeded the maximum tool loop iterations.");
            }

            var completion = await _openAiClient.CompleteAsync(messages, toolDefinitions, cancellationToken);
            var toolCalls = completion.Message.ToolCalls ?? Array.Empty<OpenAiToolCall>();
            if (toolCalls.Count == 0)
            {
                return completion;
            }

            messages.Add(new OpenAiMessage("assistant", completion.Message.Content, ToolCalls: toolCalls));

            foreach (var toolCall in toolCalls)
            {
                var toolResult = await _toolExecutor.ExecuteAsync(userId, toolCall.Function.Name, toolCall.Function.Arguments, cancellationToken);

                messages.Add(new OpenAiMessage("tool", toolResult, ToolCallId: toolCall.Id));
                _context.ChatMessages.Add(new ChatMessage
                {
                    SessionId = sessionId,
                    Role = "Tool",
                    Content = toolResult,
                    Timestamp = DateTime.UtcNow,
                    ToolName = toolCall.Function.Name,
                    ToolCallId = toolCall.Id,
                    ToolArgumentsJson = toolCall.Function.Arguments,
                    ToolResultJson = toolResult
                });
            }
        }
    }

    private async Task<List<OpenAiMessage>> BuildConversationAsync(int sessionId, CancellationToken cancellationToken)
    {
        var recentMessages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.Timestamp)
            .Take(10)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken);

        var messages = new List<OpenAiMessage>
        {
            new("system", BuildSystemPromptV2())
        };

        foreach (var chatMessage in recentMessages)
        {
            var role = chatMessage.Role.ToLowerInvariant();
            if (role == "tool")
            {
                continue;
            }

            if (role == "assistant" && string.IsNullOrWhiteSpace(chatMessage.Content))
            {
                continue;
            }

            messages.Add(new OpenAiMessage(role, chatMessage.Content));
        }

        return messages;
    }

    private static IReadOnlyList<OpenAiToolDefinition> BuildToolDefinitions()
    {
        return new[]
        {
            new OpenAiToolDefinition(
                "function",
                new OpenAiFunctionDefinition(
                    "get_account_info",
                    "Return the customer's account information and balances.",
                    JsonNode.Parse("""
                    {
                      "type": "object",
                      "properties": {
                      },
                      "additionalProperties": false
                    }
                    """)!)),
            new OpenAiToolDefinition(
                "function",
                new OpenAiFunctionDefinition(
                    "get_transactions",
                    "Fetch transactions with optional filters.",
                    JsonNode.Parse("""
                    {
                      "type": "object",
                      "properties": {
                        "type": {
                          "type": "string",
                          "enum": ["credit", "debit", "all"],
                          "description": "Filter transactions by type."
                        },
                        "days": {
                          "type": "number",
                          "description": "Number of days of history to return."
                        }
                      },
                      "additionalProperties": false
                    }
                    """)!)),
            new OpenAiToolDefinition(
                "function",
                new OpenAiFunctionDefinition(
                    "get_transactions_and_account_info",
                    "Fetch account balances and transactions together for multi-part banking questions.",
                    JsonNode.Parse("""
                    {
                      "type": "object",
                      "properties": {
                        "type": {
                          "type": "string",
                          "enum": ["credit", "debit", "all"],
                          "description": "Filter transactions by type."
                        },
                        "days": {
                          "type": "number",
                          "description": "Number of days of history to return."
                        }
                      },
                      "additionalProperties": false
                    }
                    """)!)),
            new OpenAiToolDefinition(
                "function",
                new OpenAiFunctionDefinition(
                    "get_spending_summary",
                    "Return current month spending by category for the customer.",
                    JsonNode.Parse("""
                    {
                      "type": "object",
                      "properties": {
                        "year": { "type": "integer" },
                        "month": { "type": "integer", "minimum": 1, "maximum": 12 }
                      },
                      "additionalProperties": false
                    }
                    """)!)),
            new OpenAiToolDefinition(
                "function",
                new OpenAiFunctionDefinition(
                    "get_savings_suggestions",
                    "Return savings suggestions derived from the customer's spending patterns.",
                    JsonNode.Parse("""
                    {
                      "type": "object",
                      "properties": {},
                      "additionalProperties": false
                    }
                    """)!))
        };
    }

    private static string BuildSystemPrompt()
    {
        var today = DateTime.UtcNow;
        var dayOfMonth = today.Day;
        return $"""
        You are a banking assistant for a proof-of-concept product.
        Today's date is {today:yyyy-MM-dd} (day {dayOfMonth} of this month).
        
        Use tools for account information and transactions.
        Do not invent balances, account details, or transactions.
        When the user asks about their banking data, call the appropriate tool first.
        
        Time mapping rules:
        - "this month" → use days={dayOfMonth} (days elapsed so far this month)
        - "last week" → use days=7
        - "last month" or "past month" → use days=30
        - "this week" → use days equal to the current day of the week (Mon=1 .. Sun=7)
        - If the user says a specific number of days, use that number.
        
        Keep responses concise, factual, and easy to understand.
        Format currency values in INR (₹).
        """;
    }

    private static string BuildSystemPromptV2()
    {
        var today = DateTime.UtcNow;
        var dayOfMonth = today.Day;
        return $"""
        You are a banking assistant for a proof-of-concept product.
        Today's date is {today:yyyy-MM-dd} (day {dayOfMonth} of this month).

        Use these tools for banking questions:
        - get_account_info for balance and account-detail questions
        - get_transactions for transaction-history questions
        - get_transactions_and_account_info for combined questions that need both balances and transactions

        Do not invent balances, account details, or transactions.
        When the user asks about their banking data, call the appropriate tool first.

        Time mapping rules:
        - "this month" -> use days={dayOfMonth} (days elapsed so far this month)
        - "last week" -> use days=7
        - "last month" or "past month" -> use days=30
        - "this week" -> use days equal to the current day of the week (Mon=1 .. Sun=7)
        - If the user says a specific number of days, use that number.
        - If the user asks for transaction history without a clear time range, use days=90.

        Tool selection rules:
        - balance-only questions -> get_account_info
        - transaction-only questions -> get_transactions
        - balance plus transaction questions -> get_transactions_and_account_info

        Type mapping rules:
        - credit-only questions -> type="credit"
        - debit-only questions -> type="debit"
        - mixed transaction questions -> type="all"

        Keep responses concise, factual, and easy to understand.
        Format currency values in INR.
        """;
    }

    private static string BuildLocalResponse(BankingSnapshotDto snapshot)
    {
        var topCategory = snapshot.CategorySpend.FirstOrDefault();
        var topSuggestion = snapshot.SavingsSuggestions.FirstOrDefault();

        var response = new List<string>
        {
            $"Your current total balance is {snapshot.TotalBalance:C}.",
            $"Monthly income is {snapshot.MonthlyIncome:C} and monthly spend is {snapshot.MonthlySpend:C}, so net cash flow is {snapshot.NetCashFlow:C}."
        };

        if (topCategory is not null)
        {
            response.Add($"Your biggest spending category is {topCategory.Category} at {topCategory.TotalAmount:C}.");
        }

        if (topSuggestion is not null)
        {
            response.Add($"Top saving suggestion: {topSuggestion.Title} - {topSuggestion.Reason}");
        }

        if (snapshot.SavingsSuggestions.Count > 1)
        {
            response.Add($"I found {snapshot.SavingsSuggestions.Count} savings suggestions overall.");
        }

        return string.Join(" ", response);
    }

    private static string BuildSessionTitle(string message)
    {
        var summary = message.Trim();
        if (summary.Length <= 60)
        {
            return summary;
        }

        return summary[..57] + "...";
    }
}
