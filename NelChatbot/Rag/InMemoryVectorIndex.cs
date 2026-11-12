using Microsoft.Extensions.AI;

namespace NelChatbot.Rag;

public sealed record SearchResult(DocumentChunk Chunk, double Score);

public sealed class InMemoryVectorIndex(IEmbeddingGenerator<string, Embedding<float>> embedder)
{
    private readonly List<(DocumentChunk Chunk, ReadOnlyMemory<float> Vector)> _entries = [];

    public int Count => _entries.Count;

    public async Task IngestDirectoryAsync(string directory, CancellationToken cancellationToken = default)
    {
        var chunks = Directory.EnumerateFiles(directory, "*.md")
            .SelectMany(file => DocumentChunker.Chunk(Path.GetFileName(file), File.ReadAllText(file)))
            .ToList();

        if (chunks.Count == 0) return;

        var embeddings = await embedder.GenerateAsync(
            chunks.Select(c => c.ToEmbeddingText()), cancellationToken: cancellationToken);

        for (var i = 0; i < chunks.Count; i++)
        {
            _entries.Add((chunks[i], embeddings[i].Vector));
        }
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query, int top = 3, double minScore = 0.25, CancellationToken cancellationToken = default)
    {
        var queryVector = await embedder.GenerateVectorAsync(query, cancellationToken: cancellationToken);

        return _entries
            .Select(e => new SearchResult(e.Chunk, CosineSimilarity(queryVector.Span, e.Vector.Span)))
            .Where(r => r.Score >= minScore)
            .OrderByDescending(r => r.Score)
            .Take(top)
            .ToList();
    }

    private static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
