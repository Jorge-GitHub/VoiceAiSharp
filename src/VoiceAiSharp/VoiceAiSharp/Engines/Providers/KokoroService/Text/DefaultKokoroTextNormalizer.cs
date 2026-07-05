using System.Text.RegularExpressions;

namespace VoiceAiSharp.Engines.Providers.KokoroService.Text;

internal sealed partial class DefaultKokoroTextNormalizer : IKokoroTextNormalizer
{
    public string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string normalized = WhitespaceRegex()
            .Replace(text.Trim(), " ");

        return normalized
            .Replace('\u2018', '\'')
            .Replace('\u2019', '\'');
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
