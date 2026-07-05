using PhonemizerSharp.Application;
using PhonemizerSharp.Domain.Requests;
using PhonemizerSharp.Domain.Results;
using PhonemizerSharp.Domain.Settings;
using PhonemizerSharp.Domain.Settings.Enums;
using PhonemizerSharp.Domain.Settings.Providers;
using VoiceAiSharp.Engines.Providers.KokoroService.Models;

namespace VoiceAiSharp.Engines.Providers.KokoroService.Text;

internal sealed class EspeakNgKokoroPhonemizer : IKokoroPhonemizer, IDisposable
{
    private readonly PhonemizerService phonemizer;
    private bool disposed;

    public EspeakNgKokoroPhonemizer()
    {
        this.phonemizer = new PhonemizerService(new PhonemizerSettings
        {
            Provider = PhonemizerProvider.EspeakNg,
            Runtime = PhonemizerRuntime.NativeLibrary,
            EspeakNg = new EspeakNgProviderSettings
            {
                IpaMode = 3,
                Quiet = true
            }
        });
    }

    public async Task<string> PhonemizeAsync(
        string text,
        KokoroVoiceSelection voice,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this.disposed, this);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(voice);

        PhonemizerResult result = await this.phonemizer.PhonemizeAsync(
            new PhonemizerRequest
            {
                Text = text,
                Language = voice.PhonemizerVoice
            },
            cancellationToken);

        string phonemes = result.Phonemes.Trim();

        if (string.IsNullOrWhiteSpace(phonemes))
        {
            throw new InvalidOperationException(
                "The Kokoro phonemizer returned no phonemes for the supplied text.");
        }

        return phonemes;
    }

    public void Dispose()
    {
        if (!this.disposed)
        {
            this.phonemizer.Dispose();
            this.disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
