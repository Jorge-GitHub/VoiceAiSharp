using VoiceAiSharp.Engines.Providers.PiperService.Runtime;

namespace VoiceAiSharp.Engines.Providers.PiperService.Text;

internal sealed class PiperPhonemeTokenizer
{
    private const string Pad = "_";
    private const string BeginningOfSentence = "^";
    private const string EndOfSentence = "$";

    private readonly PiperModelConfig _config;
    private readonly string[] _symbolsByLength;

    public PiperPhonemeTokenizer(PiperModelConfig config)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
        this._symbolsByLength = config.PhonemeIdMap.Keys
            .Where(static key => key is not (Pad or BeginningOfSentence or EndOfSentence))
            .OrderByDescending(static key => key.Length)
            .ToArray();
    }

    public int[] Tokenize(string phonemes)
    {
        ArgumentNullException.ThrowIfNull(phonemes);

        List<int> ids = new();

        this.AddIds(ids, BeginningOfSentence);

        int mappedPhonemeCount = 0;
        int index = 0;

        while (index < phonemes.Length)
        {
            string? symbol = this.MatchSymbol(phonemes, index);

            if (symbol is null)
            {
                index += GetCharacterLength(phonemes, index);
                continue;
            }

            this.AddIds(ids, symbol);
            this.AddIds(ids, Pad);
            mappedPhonemeCount++;
            index += symbol.Length;
        }

        this.AddIds(ids, EndOfSentence);

        if (mappedPhonemeCount == 0)
        {
            throw new InvalidOperationException(
                "Piper tokenization produced no speech tokens. "
                + "The phonemizer output did not match the Piper phoneme_id_map.");
        }

        return ids.ToArray();
    }

    private string? MatchSymbol(string text, int index)
    {
        foreach (string symbol in this._symbolsByLength)
        {
            if (index + symbol.Length <= text.Length
                && string.CompareOrdinal(text, index, symbol, 0, symbol.Length) == 0)
            {
                return symbol;
            }
        }

        return null;
    }

    private void AddIds(List<int> ids, string symbol)
    {
        if (this._config.PhonemeIdMap.TryGetValue(symbol, out int[]? symbolIds))
        {
            ids.AddRange(symbolIds);
        }
    }

    private static int GetCharacterLength(string text, int index)
    {
        return char.IsHighSurrogate(text[index])
            && index + 1 < text.Length
            && char.IsLowSurrogate(text[index + 1])
            ? 2
            : 1;
    }
}
