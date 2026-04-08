using System.Text.Json;
using System.Text.RegularExpressions;
using BankingAIBot.API.Options;
using Microsoft.Extensions.Options;

namespace BankingAIBot.API.Services;

public interface IChatQueryValidationService
{
    Task ValidateAsync(string message, CancellationToken cancellationToken = default);
}

public sealed class ChatQueryValidationService : IChatQueryValidationService
{
    private static readonly Regex BankingIntentPattern = new(
        @"\b(balance|account|accounts|transaction|transactions|deposit|withdraw|withdrawal|transfer|spend|spent|expense|expenses|income|credit|debit|merchant|category|loan|emi|savings|saving|statement|card|bank|banking|available balance|current balance|recent transaction|recent transactions|last \d+)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex DateRangePattern = new(
        @"\b(between|from)\b.+\b(to|and)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly string[] AllowedQuestionPhrases =
    {
        "how much did i spend",
        "how much have i spent",
        "show my last",
        "show my recent",
        "show me my last",
        "show me my recent",
        "what is my balance",
        "what's my balance",
        "current balance",
        "available balance"
    };

    private const double MinimumBankingConfidence = 0.65;

    private readonly OpenAiChatClient _openAiClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<ChatQueryValidationService> _logger;

    public ChatQueryValidationService(
        OpenAiChatClient openAiClient,
        IOptions<OpenAiOptions> options,
        ILogger<ChatQueryValidationService> logger)
    {
        _openAiClient = openAiClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ValidateAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty.", nameof(message));
        }

        if (_options.EnableIntentValidation && _options.IsConfigured)
        {
            var classification = await ClassifyAsync(message, cancellationToken);
            if (classification.IsBanking && classification.Confidence >= MinimumBankingConfidence)
            {
                return;
            }

            throw new ArgumentException(classification.Reason ??
                                        "Please ask a banking-related question about balances, accounts, transactions, transfers, or spending.");
        }

        if (!IsBankingQuery(message))
        {
            throw new ArgumentException(
                "Please ask a banking-related question about balances, accounts, transactions, transfers, or spending.");
        }
    }

    private async Task<IntentClassificationResult> ClassifyAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $$"""
            You are a strict intent classifier for a banking assistant.
            Classify the user's message as either banking-related or not banking-related.
            Banking-related means the user is asking about balances, accounts, transactions, transfers, deposits, withdrawals, spending, income, cards, loans, statements, or savings.
            If the user is asking for anything unrelated to banking, return non-banking.
            Reply with JSON only in this exact shape:
            {"isBanking":true,"intent":"balance|transactions|transfer|spend|account|loan|savings|card|statement|other","confidence":0.0,"reason":"short reason"}

            User message: {{message}}
            """;

            var response = await _openAiClient.CompleteAsync(
                new[]
                {
                    new OpenAiMessage("system", prompt),
                    new OpenAiMessage("user", message)
                },
                Array.Empty<OpenAiToolDefinition>(),
                temperature: 0,
                maxCompletionTokens: 120,
                cancellationToken);

            var content = response.Message.Content?.Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                return IntentClassificationResult.Unknown();
            }

            var parsed = TryParseClassification(content);
            return parsed ?? IntentClassificationResult.Unknown();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Intent classification failed, falling back to rule-based validation.");
            return IntentClassificationResult.Unknown();
        }
    }

    private static bool IsBankingQuery(string message)
    {
        var normalized = Normalize(message);
        return BankingIntentPattern.IsMatch(normalized) ||
               DateRangePattern.IsMatch(normalized) ||
               AllowedQuestionPhrases.Any(normalized.Contains);
    }

    private static IntentClassificationResult? TryParseClassification(string content)
    {
        var json = ExtractJson(content);
        if (json is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<IntentClassificationResult>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractJson(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end < start)
        {
            return null;
        }

        return content.Substring(start, end - start + 1);
    }

    private static string Normalize(string value)
    {
        return Regex.Replace(value.Trim().ToLowerInvariant(), @"\s+", " ");
    }
}

public sealed record IntentClassificationResult(
    bool IsBanking,
    string Intent,
    double Confidence,
    string Reason)
{
    public static IntentClassificationResult Unknown()
        => new(false, "other", 0, "Please ask a banking-related question about balances, accounts, transactions, transfers, or spending.");
}
