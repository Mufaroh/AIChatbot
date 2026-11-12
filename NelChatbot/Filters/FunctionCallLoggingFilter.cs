using Microsoft.SemanticKernel;

namespace NelChatbot.Filters;

public sealed class FunctionCallLoggingFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var args = string.Join(", ", context.Arguments.Select(a => $"{a.Key}: \"{a.Value}\""));
        ConsoleUi.WriteDim($"  [tool] {context.Function.PluginName}.{context.Function.Name}({args})");

        await next(context);
    }
}
