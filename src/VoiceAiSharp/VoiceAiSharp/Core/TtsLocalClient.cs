using VoiceAiSharp.Core.Domain.Audio;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Core.Engines.Repositories;

namespace VoiceAiSharp.Core;

/// <summary>
/// Consumer entry point for local text-to-speech synthesis.
/// </summary>
public sealed class TtsLocalClient : IDisposable
{
    private readonly EngineRepository _repository = new();
    private readonly TtsLocalClientSettings _settings;
    private bool _disposed;

    public TtsLocalClient() : this(new TtsLocalClientSettings()) { }

    public TtsLocalClient(TtsLocalClientSettings settings)
    {
        this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public Task<TtsAudio> SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);

        return this._repository
            .GetEngine(this._settings)
            .SpeakAsync(text, this._settings, cancellationToken);
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;
        this._repository.Dispose();
    }
}
