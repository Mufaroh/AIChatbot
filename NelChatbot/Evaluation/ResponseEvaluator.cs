using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace NelChatbot.Evaluation;

public sealed record EvalCase(string Question, string[] ExpectedFacts);

public sealed record EvalResult(EvalCase Case, string Answer, int Score, string Reasoning);

public sealed class ResponseEvaluator(Kernel kernel, ChatbotSettings settings, string systemPrompt, string judgePrompt)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<EvalCase> LoadCases(string path) =>
        JsonSerializer.Deserialize<List<EvalCase>>(File.ReadAllText(path), JsonOptions)
        ?? throw new InvalidOperationException($"No evaluation cases found in {path}");

    public async Task<IReadOnlyList<EvalResult>> RunAsync(
        IEnumerable<EvalCase> cases, CancellationToken cancellationToken = default)
    {
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var results = new List<EvalResult>();

        foreach (var evalCase in cases)
        {
            // Each question gets a fresh conversation so earlier answers can't leak in.
            var history = new ChatHistory(systemPrompt);
            history.AddUserMessage(evalCase.Question);
            var answer = await chat.GetChatMessageContentAsync(
                history, settings.CreateChatExecutionSettings(), kernel, cancellationToken);

            var (score, reasoning) = await JudgeAsync(chat, evalCase, answer.Content ?? string.Empty, cancellationToken);
            results.Add(new EvalResult(evalCase, answer.Content ?? string.Empty, score, reasoning));
        }

        return results;
    }

    private async Task<(int Score, string Reasoning)> JudgeAsync(
        IChatCompletionService chat, EvalCase evalCase, string answer, CancellationToken cancellationToken)
    {
        var judgeHistory = new ChatHistory(judgePrompt);
        judgeHistory.AddUserMessage(
            $"""
             Question:
             {evalCase.Question}

             Expected facts:
             {string.Join("\n", evalCase.ExpectedFacts.Select(f => $"- {f}"))}

             Assistant answer:
             {answer}
             """);

        // No kernel passed in, so the judge can't call tools; JSON mode makes the output parseable.
        var verdict = await chat.GetChatMessageContentAsync(
            judgeHistory,
            new OpenAIPromptExecutionSettings { Temperature = 0, ResponseFormat = "json_object" },
            cancellationToken: cancellationToken);

        try
        {
            using var doc = JsonDocument.Parse(verdict.Content ?? "{}");
            return (doc.RootElement.GetProperty("score").GetInt32(),
                    doc.RootElement.GetProperty("reasoning").GetString() ?? string.Empty);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return (0, $"Could not parse judge output: {verdict.Content}");
        }
    }
}
