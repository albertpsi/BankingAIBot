namespace BankingAIBot.API.Options;

public sealed class OpenAiOptions
{
    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-5.4-mini";
    public double Temperature { get; set; } = 0.2;
    public int MaxCompletionTokens { get; set; } = 800;
    public string PromptVersion { get; set; } = "poc-v1";
    public bool EnableIntentValidation { get; set; } = false;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
