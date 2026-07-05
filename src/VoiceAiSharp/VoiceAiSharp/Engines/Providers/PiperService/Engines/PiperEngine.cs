using VoiceAiSharp.Core.Audio;
using VoiceAiSharp.Core.Domain.Audio;
using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Core.Domain.Settings.Engines;
using VoiceAiSharp.Core.Engines.Base;
using VoiceAiSharp.Engines.Providers.PiperService.Models;
using VoiceAiSharp.Engines.Providers.PiperService.Runtime;
using VoiceAiSharp.Engines.Providers.PiperService.Text;
using VoiceAiSharp.Engines.Providers.PiperService.Voices;

namespace VoiceAiSharp.Engines.Providers.PiperService.Engines;

internal sealed class PiperEngine : TtsEngineBase
{
    private const double InterChunkSilenceSeconds = 0.1d;

    private readonly TtsRuntimeSettings _runtimeSettings;
    private readonly IPiperTextNormalizer _normalizer = new DefaultPiperTextNormalizer();
    private readonly IPiperTextChunker _chunker = new DefaultPiperTextChunker();
    private readonly IPiperPhonemizer _phonemizer = new EspeakNgPiperPhonemizer();
    private readonly TtsAudioEncoder _audioEncoder = new();
    private readonly object _runtimeLock = new();
    private PiperOnnxRuntime? _runtime;

    public PiperEngine(TtsLocalClientSettings settings, TtsBackend backend)
        : base(TtsEngineType.Piper, backend, settings.Runtime)
    {
        ArgumentNullException.ThrowIfNull(settings);

        this._runtimeSettings = new TtsRuntimeSettings
        {
            Engine = TtsEngineType.Piper,
            Backend = backend,
            ModelPath = settings.Runtime.ModelPath,
            ModelDirectory = settings.Runtime.ModelDirectory,
            ModelName = settings.Runtime.ModelName,
            Threads = settings.Runtime.Threads
        };
    }

    public override async Task<TtsAudio> SpeakAsync(
        string text,
        TtsLocalClientSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(text))
        {
            return this._audioEncoder.CreateEmpty("", settings);
        }

        PiperOnnxRuntime runtime = this.GetRuntime();
        PiperVoiceSelection voice = PiperVoiceResolver.Resolve(
            settings.Voice,
            runtime.Config);

        PiperPhonemeTokenizer tokenizer = new(runtime.Config);
        string normalizedText = this._normalizer.Normalize(text);
        IReadOnlyList<string> chunks = this._chunker.Chunk(normalizedText);
        List<float> samples = new();

        for (int i = 0; i < chunks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string phonemes = await this.CreatePhonemesAsync(
                chunks[i],
                voice,
                runtime.Config,
                cancellationToken);

            int[] phonemeIds = tokenizer.Tokenize(phonemes);
            float[] segment = runtime.Synthesize(
                phonemeIds,
                settings.Voice.Speed,
                voice.SpeakerId);

            this.AppendSamples(samples, segment);

            if (i < chunks.Count - 1)
            {
                this.AppendSilence(samples, runtime.Config.SampleRate);
            }
        }

        return this._audioEncoder.Create(
            samples.ToArray(),
            normalizedText,
            settings,
            voice.Language,
            voice.Gender,
            runtime.ModelName,
            runtime.Config.SampleRate);
    }

    public override void Dispose()
    {
        lock (this._runtimeLock)
        {
            this._runtime?.Dispose();
            this._runtime = null;
        }

        if (this._phonemizer is IDisposable disposablePhonemizer)
        {
            disposablePhonemizer.Dispose();
        }

        base.Dispose();
    }

    private async Task<string> CreatePhonemesAsync(
        string text,
        PiperVoiceSelection voice,
        PiperModelConfig config,
        CancellationToken cancellationToken)
    {
        if (config.PhonemeType == PiperPhonemeType.Text)
        {
            return text;
        }

        return await this._phonemizer.PhonemizeAsync(
            text,
            voice,
            cancellationToken);
    }

    private PiperOnnxRuntime GetRuntime()
    {
        lock (this._runtimeLock)
        {
            this._runtime ??= new PiperOnnxRuntime(this._runtimeSettings);
            return this._runtime;
        }
    }

    private void AppendSamples(List<float> target, float[] samples)
    {
        foreach (float sample in samples)
        {
            target.Add(Math.Clamp(sample, -1f, 1f));
        }
    }

    private void AppendSilence(List<float> target, int sampleRate)
    {
        int sampleCount = (int)Math.Round(sampleRate * InterChunkSilenceSeconds);

        for (int i = 0; i < sampleCount; i++)
        {
            target.Add(0f);
        }
    }
}
