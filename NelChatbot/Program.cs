using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NelChatbot;
using NelChatbot.Evaluation;
using NelChatbot.Filters;
using NelChatbot.Plugins;
using NelChatbot.Rag;

Console.OutputEncoding = Encoding.UTF8;

ChatbotSettings settings;
try
{
    var config = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build();
    settings = ChatbotSettings.Load(config);
}
catch (InvalidOperationException ex)
{
    ConsoleUi.WriteError(ex.Message);
    return 1;
}

// Kernel setup: register OpenAI services with Semantic Kernel
var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(settings.ChatModel, settings.ApiKey);
#pragma warning disable SKEXP0010 // Embedding generator registration is marked experimental in SK.
builder.AddOpenAIEmbeddingGenerator(settings.EmbeddingModel, settings.ApiKey);
#pragma warning restore SKEXP0010
var kernel = builder.Build();

//  Retrieval: embed the knowledge base once at startup 
var index = new InMemoryVectorIndex(kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());
ConsoleUi.WriteDim("Indexing knowledge base...");
await index.IngestDirectoryAsync(Path.Combine(AppContext.BaseDirectory, "Data", "Knowledge"));
ConsoleUi.WriteDim($"Indexed {index.Count} chunks.");

//Plugins: tools the model can choose to call
kernel.Plugins.AddFromObject(new KnowledgeBasePlugin(index), "KnowledgeBase");
kernel.Plugins.AddFromType<DateTimePlugin>("DateTime");

var systemPrompt = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Prompts", "SystemPrompt.txt"));

if (args.Contains("--eval"))
{
    return await RunEvaluationAsync();
}

kernel.FunctionInvocationFilters.Add(new FunctionCallLoggingFilter());
await RunChatAsync();
return 0;

async Task RunChatAsync()
{
    var chat = kernel.GetRequiredService<IChatCompletionService>();
    var executionSettings = settings.CreateChatExecutionSettings();
    var history = new ChatHistory(systemPrompt);

    Console.WriteLine();
    Console.WriteLine($"Nelisiwe Bikes assistant ({settings.ChatModel}). Type /reset to start over, /exit to quit.");

    while (true)
    {
        Console.WriteLine();
        ConsoleUi.WriteColored("You > ", ConsoleColor.Cyan, newLine: false);
        var input = Console.ReadLine()?.Trim();

        if (input is null || input.Equals("/exit", StringComparison.OrdinalIgnoreCase)) break;
        if (input.Length == 0) continue;
        if (input.Equals("/reset", StringComparison.OrdinalIgnoreCase))
        {
            history = new ChatHistory(systemPrompt);
            ConsoleUi.WriteDim("Conversation cleared.");
            continue;
        }

        history.AddUserMessage(input);

        try
        {
            var reply = new StringBuilder();
            var started = false;

            // Streaming with auto function calling: SK runs any tool calls the model
            // requests, feeds the results back, and streams the final answer.
            await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(history, executionSettings, kernel))
            {
                if (string.IsNullOrEmpty(chunk.Content)) continue;
                if (!started)
                {
                    ConsoleUi.WriteColored("Bot > ", ConsoleColor.Green, newLine: false);
                    started = true;
                }
                Console.Write(chunk.Content);
                reply.Append(chunk.Content);
            }

            Console.WriteLine();
            history.AddAssistantMessage(reply.ToString());
        }
        catch (HttpOperationException ex)
        {
            ConsoleUi.WriteError($"OpenAI request failed: {ex.Message}");
            history.RemoveAt(history.Count - 1);
        }
    }
}

async Task<int> RunEvaluationAsync()
{
    var judgePrompt = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Prompts", "JudgePrompt.txt"));
    var cases = ResponseEvaluator.LoadCases(Path.Combine(AppContext.BaseDirectory, "Data", "eval-set.json"));
    var evaluator = new ResponseEvaluator(kernel, settings, systemPrompt, judgePrompt);

    Console.WriteLine($"Evaluating {cases.Count} questions with {settings.ChatModel}...\n");
    var results = await evaluator.RunAsync(cases);

    foreach (var r in results)
    {
        var color = r.Score >= 4 ? ConsoleColor.Green : r.Score == 3 ? ConsoleColor.Yellow : ConsoleColor.Red;
        ConsoleUi.WriteColored($"[{r.Score}/5] {r.Case.Question}", color);
        ConsoleUi.WriteDim($"  Answer:    {r.Answer.ReplaceLineEndings(" ")}");
        ConsoleUi.WriteDim($"  Reasoning: {r.Reasoning}");
        Console.WriteLine();
    }

    Console.WriteLine($"Average score: {results.Average(r => r.Score):0.00} / 5");
    return 0;
}
