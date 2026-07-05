using VoiceAiSharp.Core.Audio;
using VoiceAiSharp.Core.Domain.Audio;
using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Core.Domain.Settings.Engines;
using VoiceAiSharp.Core.Engines.Base;
using VoiceAiSharp.Engines.Providers.KokoroService.Models;
using VoiceAiSharp.Engines.Providers.KokoroService.Runtime;
using VoiceAiSharp.Engines.Providers.KokoroService.Text;
using VoiceAiSharp.Engines.Providers.KokoroService.Voices;

namespace VoiceAiSharp.Engines.Providers.KokoroService.Engines;

internal sealed class KokoroEngine : TtsEngineBase
{
    private const int InterChunkSilenceSamples = 2400;

    private readonly TtsRuntimeSettings _runtimeSettings;
    private readonly IKokoroTextNormalizer _normalizer = new DefaultKokoroTextNormalizer();
    private readonly IKokoroTextChunker _chunker = new DefaultKokoroTextChunker();
    private readonly IKokoroPhonemizer _phonemizer = new EspeakNgKokoroPhonemizer();
    private readonly KokoroTokenizer _tokenizer = new();
    private readonly TtsAudioEncoder _audioEncoder = new();
    private readonly object _runtimeLock = new();
    private KokoroOnnxRuntime? _runtime;

    public KokoroEngine(TtsLocalClientSettings settings, TtsBackend backend)
        : base(TtsEngineType.Kokoro, backend, settings.Runtime)
    {
        ArgumentNullException.ThrowIfNull(settings);

        this._runtimeSettings = new TtsRuntimeSettings
        {
            Engine = TtsEngineType.Kokoro,
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

        KokoroVoiceSelection voice = KokoroVoicePresetResolver.Resolve(settings.Voice);
        string normalizedText = this._normalizer.Normalize(text);
        List<float> samples = new();
        IReadOnlyList<string> chunks = this._chunker.Chunk(normalizedText);

        for (int i = 0; i < chunks.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string phonemes = await this._phonemizer.PhonemizeAsync(
                chunks[i],
                voice,
                cancellationToken);

            int[] tokens = this._tokenizer.Tokenize(phonemes);
            float[] segment = this.GetRuntime().Synthesize(
                tokens,
                voice.VoiceId,
                settings.Voice.Speed);

            this.AppendSamples(samples, segment);

            if (i < chunks.Count - 1)
            {
                this.AppendSilence(samples, InterChunkSilenceSamples);
            }
        }

        float[] monoSamples = samples.ToArray();

        return this._audioEncoder.Create(
            monoSamples,
            normalizedText,
            settings,
            voice.Language,
            voice.Gender,
            this.GetRuntime().ModelName);
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

    private KokoroOnnxRuntime GetRuntime()
    {
        lock (this._runtimeLock)
        {
            this._runtime ??= new KokoroOnnxRuntime(this._runtimeSettings);
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

    private void AppendSilence(List<float> target, int sampleCount)
    {
        for (int i = 0; i < sampleCount; i++)
        {
            target.Add(0f);
        }
    }
}
