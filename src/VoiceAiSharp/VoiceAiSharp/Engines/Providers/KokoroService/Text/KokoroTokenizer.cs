using System.Text;

namespace VoiceAiSharp.Engines.Providers.KokoroService.Text;

internal sealed class KokoroTokenizer
{
    private readonly IReadOnlyDictionary<string, int> _vocabulary;

    public KokoroTokenizer() : this(KokoroVocabulary.CreateDefault())
    {
    }

    public KokoroTokenizer(IReadOnlyDictionary<string, int> vocabulary)
    {
        this._vocabulary = vocabulary
            ?? throw new ArgumentNullException(nameof(vocabulary));
    }

    public int[] Tokenize(string phonemes)
    {
        ArgumentNullException.ThrowIfNull(phonemes);

        List<int> tokens = new(capacity: phonemes.Length + 2) { 0 };

        foreach (Rune rune in phonemes.EnumerateRunes())
        {
            string value = rune.ToString();

            if (this._vocabulary.TryGetValue(value, out int token))
            {
                tokens.Add(token);
            }
        }

        tokens.Add(0);

        if (tokens.Count == 2)
        {
            throw new InvalidOperationException(
                "Kokoro tokenization produced no speech tokens. "
                + "The phonemizer output did not match the Kokoro vocabulary.");
        }

        return tokens.ToArray();
    }
}
