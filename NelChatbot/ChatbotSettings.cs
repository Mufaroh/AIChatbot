using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace NelChatbot;

public sealed record ChatbotSettings(string ApiKey, string ChatModel, string EmbeddingModel, double Temperature)
{
    public static ChatbotSettings Load(IConfiguration config)
    {
        var apiKey = config["OpenAI:ApiKey"] ?? config["OPENAI_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key not found. Set it with:\n" +
                "  dotnet user-secrets set \"OpenAI:ApiKey\" \"sk-...\"\n" +
                "or set the OPENAI_API_KEY environment variable.");
        }

        return new ChatbotSettings(
            apiKey,
            config["OpenAI:ChatModel"] ?? "gpt-4.1-mini",
            config["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small",
            double.TryParse(config["OpenAI:Temperature"], out var t) ? t : 0.2);
    }

    public OpenAIPromptExecutionSettings CreateChatExecutionSettings() => new()
    {
        Temperature = Temperature,
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    };
}
