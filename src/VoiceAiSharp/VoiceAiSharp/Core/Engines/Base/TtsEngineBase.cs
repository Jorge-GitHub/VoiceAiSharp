using VoiceAiSharp.Core.Domain.Audio;
using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Core.Domain.Settings.Engines;

namespace VoiceAiSharp.Core.Engines.Base;

internal abstract class TtsEngineBase : ITtsEngine
{
    public TtsEngineType Type { get; }
    public TtsBackend Backend { get; }
    public string ModelPath { get; }
    public string ModelDirectory { get; }
    public string ModelName { get; }
    public int Threads { get; }

    protected TtsEngineBase(
        TtsEngineType type,
        TtsBackend backend,
        TtsRuntimeSettings runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        this.Type = type;
        this.Backend = backend;
        this.ModelPath = runtime.ModelPath;
        this.ModelDirectory = runtime.ModelDirectory;
        this.ModelName = runtime.ModelName;
        this.Threads = runtime.Threads;
    }

    public abstract Task<TtsAudio> SpeakAsync(
        string text,
        TtsLocalClientSettings settings,
        CancellationToken cancellationToken);

    public virtual void Dispose()
    {
    }
}
