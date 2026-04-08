using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BankingAIBot.API.Options;
using Microsoft.Extensions.Options;

namespace BankingAIBot.API.Services;

public sealed class OpenAiChatClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiChatClient> _logger;

    public OpenAiChatClient(HttpClient httpClient, IOptions<OpenAiOptions> options, ILogger<OpenAiChatClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OpenAiCompletionResult> CompleteAsync(
        IReadOnlyList<OpenAiMessage> messages,
        IReadOnlyList<OpenAiToolDefinition> tools,
        CancellationToken cancellationToken = default)
        => await CompleteAsync(messages, tools, null, null, cancellationToken);

    public async Task<OpenAiCompletionResult> CompleteAsync(
        IReadOnlyList<OpenAiMessage> messages,
        IReadOnlyList<OpenAiToolDefinition> tools,
        double? temperature,
        int? maxCompletionTokens,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_options.IsConfigured)
            {
                throw new InvalidOperationException("OpenAI is not configured.");
            }

            var request = new OpenAiChatCompletionRequest(
                _options.Model,
                messages,
                tools.Count == 0 ? null : tools,
                "auto",
                temperature ?? _options.Temperature,
                maxCompletionTokens ?? _options.MaxCompletionTokens);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
            {
                Content = JsonContent.Create(request, options: JsonOptions)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"OpenAI request failed with status {(int)response.StatusCode}: {rawJson}");
            }

            var parsed = JsonSerializer.Deserialize<OpenAiChatCompletionResponse>(rawJson, JsonOptions)
                ?? throw new InvalidOperationException("OpenAI returned an empty response.");

            var choice = parsed.Choices.FirstOrDefault()
                ?? throw new InvalidOperationException("OpenAI response did not include a choice.");

            return new OpenAiCompletionResult(choice.Message, parsed.Usage, rawJson);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "OpenAI completion request failed.");
            throw;
        }
    }
}

public sealed record OpenAiCompletionResult(
    OpenAiMessage Message,
    OpenAiUsage? Usage,
    string RawJson);

public sealed record OpenAiChatCompletionRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<OpenAiMessage> Messages,
    [property: JsonPropertyName("tools")] IReadOnlyList<OpenAiToolDefinition>? Tools,
    [property: JsonPropertyName("tool_choice")] string ToolChoice,
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("max_completion_tokens")] int MaxCompletionTokens);

public sealed record OpenAiMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string? Content,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("tool_call_id")] string? ToolCallId = null,
    [property: JsonPropertyName("tool_calls")] IReadOnlyList<OpenAiToolCall>? ToolCalls = null);

public sealed record OpenAiToolDefinition(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("function")] OpenAiFunctionDefinition Function);

public sealed record OpenAiFunctionDefinition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("parameters")] JsonNode Parameters);

public sealed record OpenAiToolCall(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("function")] OpenAiFunctionCall Function);

public sealed record OpenAiFunctionCall(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] string Arguments);

public sealed record OpenAiChatCompletionResponse(
    [property: JsonPropertyName("choices")] IReadOnlyList<OpenAiChoice> Choices,
    [property: JsonPropertyName("usage")] OpenAiUsage? Usage);

public sealed record OpenAiChoice(
    [property: JsonPropertyName("message")] OpenAiMessage Message);

public sealed record OpenAiUsage(
    [property: JsonPropertyName("prompt_tokens")] int? PromptTokens,
    [property: JsonPropertyName("completion_tokens")] int? CompletionTokens,
    [property: JsonPropertyName("total_tokens")] int? TotalTokens);
