using System.Text;

namespace NelChatbot.Rag;

public sealed record DocumentChunk(string Source, string Heading, string Text)
{
    public string ToEmbeddingText() => string.IsNullOrEmpty(Heading) ? Text : $"{Heading}\n{Text}";
}

public static class DocumentChunker
{
    public static IEnumerable<DocumentChunk> Chunk(string source, string markdown, int maxChars = 800)
    {
        var heading = string.Empty;
        var buffer = new StringBuilder();

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            if (line.StartsWith('#'))
            {
                if (TryFlush(out var chunk)) yield return chunk;
                heading = line.TrimStart('#').Trim();
                continue;
            }

            if (buffer.Length + line.Length > maxChars && TryFlush(out var full))
            {
                yield return full;
            }

            buffer.AppendLine(line);
        }

        if (TryFlush(out var last)) yield return last;

        bool TryFlush(out DocumentChunk chunk)
        {
            var text = buffer.ToString().Trim();
            buffer.Clear();
            chunk = new DocumentChunk(source, heading, text);
            return text.Length > 0;
        }
    }
}
