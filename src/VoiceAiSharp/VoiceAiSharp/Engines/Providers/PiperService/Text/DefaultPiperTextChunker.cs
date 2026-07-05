namespace VoiceAiSharp.Engines.Providers.PiperService.Text;

internal sealed class DefaultPiperTextChunker : IPiperTextChunker
{
    private const int MaxCharacters = 350;

    public IReadOnlyList<string> Chunk(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length <= MaxCharacters)
        {
            return [text];
        }

        List<string> chunks = new();
        int start = 0;

        while (start < text.Length)
        {
            int length = Math.Min(MaxCharacters, text.Length - start);
            int end = this.FindBreak(text, start, length);

            string chunk = text[start..end].Trim();
            if (chunk.Length > 0)
            {
                chunks.Add(chunk);
            }

            start = end;
        }

        return chunks;
    }

    private int FindBreak(string text, int start, int length)
    {
        int hardEnd = start + length;

        if (hardEnd >= text.Length)
        {
            return text.Length;
        }

        for (int i = hardEnd - 1; i > start; i--)
        {
            if (this.IsSentenceBoundary(text[i]) || char.IsWhiteSpace(text[i]))
            {
                return i + 1;
            }
        }

        return hardEnd;
    }

    private bool IsSentenceBoundary(char value)
        => value is '.' or '!' or '?' or ';' or ':';
}
