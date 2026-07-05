using VoiceAiSharp.Engines.Providers.PiperService.Models;
using Avalon.Service.Phonemizer.Application;
using Avalon.Service.Phonemizer.Domain.Requests;
using Avalon.Service.Phonemizer.Domain.Results;
using Avalon.Service.Phonemizer.Domain.Settings;
using Avalon.Service.Phonemizer.Domain.Settings.Enums;
using Avalon.Service.Phonemizer.Domain.Settings.Providers;

namespace VoiceAiSharp.Engines.Providers.PiperService.Text;

internal sealed class EspeakNgPiperPhonemizer : IPiperPhonemizer, IDisposable
{
    private readonly PhonemizerService _phonemizer;
    private bool _disposed;

    public EspeakNgPiperPhonemizer()
    {
        this._phonemizer = new PhonemizerService(new PhonemizerSettings
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
        PiperVoiceSelection voice,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(voice);

        PhonemizerResult result = await this._phonemizer.PhonemizeAsync(
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
                "The Piper phonemizer returned no phonemes for the supplied text.");
        }

        return phonemes;
    }

    public void Dispose()
    {
        if (!this._disposed)
        {
            this._phonemizer.Dispose();
            this._disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
