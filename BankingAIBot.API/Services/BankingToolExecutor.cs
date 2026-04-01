using System.Text.Json;
using BankingAIBot.API.Contracts;

namespace BankingAIBot.API.Services;

public interface IBankingToolExecutor
{
    Task<string> ExecuteAsync(int userId, string toolName, string argumentsJson, CancellationToken cancellationToken = default);
}

public sealed class BankingToolExecutor : IBankingToolExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly IBankingToolDataService _toolDataService;
    private readonly IBankingInsightsService _insightsService;

    public BankingToolExecutor(IBankingToolDataService toolDataService, IBankingInsightsService insightsService)
    {
        _toolDataService = toolDataService;
        _insightsService = insightsService;
    }

    public async Task<string> ExecuteAsync(int userId, string toolName, string argumentsJson, CancellationToken cancellationToken = default)
    {
        var normalizedName = toolName.Trim().ToLowerInvariant();
        var args = ParseArguments(argumentsJson);

        return normalizedName switch
        {
            "get_account_info" or "get_account_overview" => JsonSerializer.Serialize(
                await _toolDataService.GetAccountInfoAsync(userId, cancellationToken),
                JsonOptions),
            "get_transactions" or "get_recent_transactions" => JsonSerializer.Serialize(
                await _toolDataService.GetTransactionsAsync(
                    userId,
                    GetString(args, "type", "all"),
                    GetLookbackDays(args, 90),
                    cancellationToken),
                JsonOptions),
            "get_transactions_and_account_info" => JsonSerializer.Serialize(
                await _toolDataService.GetTransactionsAndAccountInfoAsync(
                    userId,
                    GetString(args, "type", "all"),
                    GetLookbackDays(args, 90),
                    cancellationToken),
                JsonOptions),
            "get_spending_summary" => JsonSerializer.Serialize(
                new
                {
                    year = GetInt(args, "year", DateTime.UtcNow.Year),
                    month = GetInt(args, "month", DateTime.UtcNow.Month),
                    categories = await _insightsService.GetMonthlyCategorySpendAsync(
                        userId,
                        GetInt(args, "year", DateTime.UtcNow.Year),
                        GetInt(args, "month", DateTime.UtcNow.Month),
                        cancellationToken)
                },
                JsonOptions),
            "get_savings_suggestions" => JsonSerializer.Serialize(
                new
                {
                    suggestions = await _insightsService.GetSavingsSuggestionsAsync(userId, cancellationToken)
                },
                JsonOptions),
            _ => JsonSerializer.Serialize(
                new
                {
                    error = $"Unknown tool '{toolName}'."
                },
                JsonOptions)
        };
    }

    private static Dictionary<string, JsonElement> ParseArguments(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        }

        using var document = JsonDocument.Parse(argumentsJson);
        return document.RootElement
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone(), StringComparer.OrdinalIgnoreCase);
    }

    private static int GetLookbackDays(Dictionary<string, JsonElement> args, int fallback)
        => GetInt(args, "days", fallback);

    private static string GetString(Dictionary<string, JsonElement> args, string key, string fallback)
    {
        if (!args.TryGetValue(key, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String when !string.IsNullOrWhiteSpace(value.GetString()) => value.GetString()!,
            _ => fallback
        };
    }

    private static int GetInt(Dictionary<string, JsonElement> args, string key, int fallback)
    {
        if (!args.TryGetValue(key, out var value))
        {
            return fallback;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => fallback
        };
    }
}
