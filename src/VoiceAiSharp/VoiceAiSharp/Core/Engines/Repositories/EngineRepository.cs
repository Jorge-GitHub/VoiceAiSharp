using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Core.Domain.Settings.Engines;
using VoiceAiSharp.Core.Engines.Base;
using VoiceAiSharp.Engines.Providers.KokoroService.FactoryService;
using VoiceAiSharp.Engines.Providers.PiperService.FactoryService;

namespace VoiceAiSharp.Core.Engines.Repositories;

internal sealed class EngineRepository : IDisposable
{
    private readonly List<TtsEngineBase> _engines = new();
    private readonly object _lock = new();
    private bool _disposed;

    public int Count
    {
        get
        {
            lock (this._lock)
            {
                return this._engines.Count;
            }
        }
    }

    public TtsEngineBase GetEngine(TtsLocalClientSettings settings)
    {
        ObjectDisposedException.ThrowIf(this._disposed, this);
        ArgumentNullException.ThrowIfNull(settings);

        TtsEngineType engineType = this.ResolveEngineType(settings.Runtime.Engine);
        TtsBackend backend = this.ResolveBackend(settings.Runtime.Backend);

        lock (this._lock)
        {
            TtsEngineBase? engine = this.GetCachedEngine(settings.Runtime, engineType, backend);

            if (engine is null)
            {
                engine = this.CreateEngine(settings, engineType, backend);
                this._engines.Add(engine);
            }

            return engine;
        }
    }

    private TtsEngineType ResolveEngineType(TtsEngineType engineType)
    {
        return engineType switch
        {
            TtsEngineType.Auto => TtsEngineType.Kokoro,
            TtsEngineType.Kokoro => TtsEngineType.Kokoro,
            TtsEngineType.Piper => TtsEngineType.Piper,
            _ => throw new NotImplementedException(
                $"TTS engine '{engineType}' is not implemented yet.")
        };
    }

    private TtsBackend ResolveBackend(TtsBackend backend)
    {
        return backend switch
        {
            TtsBackend.Auto => TtsBackend.Cpu,
            TtsBackend.Cpu => TtsBackend.Cpu,
            TtsBackend.Cuda => TtsBackend.Cuda,
            TtsBackend.DirectML => TtsBackend.DirectML,
            _ => throw new NotImplementedException(
                $"TTS backend '{backend}' is not implemented yet.")
        };
    }

    private TtsEngineBase? GetCachedEngine(
        TtsRuntimeSettings runtime,
        TtsEngineType engineType,
        TtsBackend backend)
    {
        foreach (TtsEngineBase engine in this._engines)
        {
            if (engine.Type == engineType
                && engine.Backend == backend
                && engine.ModelPath == runtime.ModelPath
                && engine.ModelDirectory == runtime.ModelDirectory
                && engine.ModelName == runtime.ModelName
                && engine.Threads == runtime.Threads)
            {
                return engine;
            }
        }

        return null;
    }

    private TtsEngineBase CreateEngine(
        TtsLocalClientSettings settings,
        TtsEngineType engineType,
        TtsBackend backend)
    {
        return engineType switch
        {
            TtsEngineType.Kokoro => new KokoroEngineFactory().Create(settings, backend),
            TtsEngineType.Piper => new PiperEngineFactory().Create(settings, backend),
            _ => throw new NotImplementedException(
                $"TTS engine '{engineType}' is not implemented yet.")
        };
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;

        foreach (TtsEngineBase engine in this._engines)
        {
            engine.Dispose();
        }
    }
}
