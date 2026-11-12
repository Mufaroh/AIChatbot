using System.ComponentModel;
using Microsoft.SemanticKernel;
using NelChatbot.Rag;

namespace NelChatbot.Plugins;
public sealed class KnowledgeBasePlugin(InMemoryVectorIndex index)
{
    [KernelFunction("search")]
    [Description("Searches the Nelisiwe Bikes knowledge base (products, warranty, shipping, returns, servicing). " +
                 "Always use this before answering any question about Nelisiwe Bikes.")]
    public async Task<string> SearchAsync(
        [Description("A short, focused search query, e.g. 'battery warranty length'")] string query,
        CancellationToken cancellationToken = default)
    {
        var results = await index.SearchAsync(query, cancellationToken: cancellationToken);

        if (results.Count == 0)
        {
            return "No relevant information found in the knowledge base.";
        }

        return string.Join("\n\n---\n\n", results.Select(r =>
            $"[Source: {r.Chunk.Source} > {r.Chunk.Heading}]\n{r.Chunk.Text}"));
    }
}
