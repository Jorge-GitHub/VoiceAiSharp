using VoiceAiSharp.Core.Domain.Settings.Engines;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace VoiceAiSharp.Engines.Providers.PiperService.Runtime;

internal sealed class PiperOnnxRuntime : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly string _inputLengthsName;
    private readonly string _scalesName;
    private readonly string? _speakerIdName;
    private readonly Type _inputType;
    private readonly Type _inputLengthsType;
    private readonly Type _scalesType;
    private readonly Type? _speakerIdType;
    private readonly object _lock = new();

    public string ModelName { get; }
    public PiperModelConfig Config { get; }

    public PiperOnnxRuntime(TtsRuntimeSettings runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        PiperModelFiles files = PiperModelResolver.Resolve(runtime);
        SessionOptions options = new PiperSessionOptionsFactory().Create(runtime);

        this._session = new InferenceSession(files.ModelPath, options);
        this.Config = PiperModelConfig.Load(files.ConfigPath);
        this.ModelName = string.IsNullOrWhiteSpace(runtime.ModelName)
            ? Path.GetFileNameWithoutExtension(files.ModelPath)
            : runtime.ModelName;

        this._inputName = this.FindInputName("input");
        this._inputLengthsName = this.FindInputName("input_lengths");
        this._scalesName = this.FindInputName("scales");
        this._speakerIdName = this.FindOptionalInputName("sid");
        this._inputType = this._session.InputMetadata[this._inputName].ElementType;
        this._inputLengthsType = this._session.InputMetadata[this._inputLengthsName].ElementType;
        this._scalesType = this._session.InputMetadata[this._scalesName].ElementType;
        this._speakerIdType = this._speakerIdName is null
            ? null
            : this._session.InputMetadata[this._speakerIdName].ElementType;
    }

    public float[] Synthesize(int[] phonemeIds, float speed, int? speakerId)
    {
        ArgumentNullException.ThrowIfNull(phonemeIds);

        if (phonemeIds.Length == 0)
        {
            throw new ArgumentException("Phoneme id list cannot be empty.", nameof(phonemeIds));
        }

        if (!float.IsFinite(speed) || speed <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(speed),
                "Voice speed must be a finite value greater than zero.");
        }

        List<NamedOnnxValue> inputs =
        [
            this.CreateInput(phonemeIds),
            this.CreateInputLengths(phonemeIds.Length),
            this.CreateScales(speed)
        ];

        if (this._speakerIdName is not null)
        {
            inputs.Add(this.CreateSpeakerId(speakerId ?? 0));
        }

        lock (this._lock)
        {
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results =
                this._session.Run(inputs);

            float[] audio = results.First().AsTensor<float>().ToArray();

            return Normalize(audio);
        }
    }

    public void Dispose()
    {
        this._session.Dispose();
    }

    private NamedOnnxValue CreateInput(int[] phonemeIds)
    {
        if (this._inputType == typeof(long))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._inputName,
                new DenseTensor<long>(
                    phonemeIds.Select(id => (long)id).ToArray(),
                    new[] { 1, phonemeIds.Length }));
        }

        if (this._inputType == typeof(int))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._inputName,
                new DenseTensor<int>(phonemeIds, new[] { 1, phonemeIds.Length }));
        }

        throw new NotSupportedException(
            $"Piper input tensor type '{this._inputType}' is not supported.");
    }

    private NamedOnnxValue CreateInputLengths(int phonemeIdCount)
    {
        if (this._inputLengthsType == typeof(long))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._inputLengthsName,
                new DenseTensor<long>(new[] { (long)phonemeIdCount }, new[] { 1 }));
        }

        if (this._inputLengthsType == typeof(int))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._inputLengthsName,
                new DenseTensor<int>(new[] { phonemeIdCount }, new[] { 1 }));
        }

        throw new NotSupportedException(
            $"Piper input_lengths tensor type '{this._inputLengthsType}' is not supported.");
    }

    private NamedOnnxValue CreateScales(float speed)
    {
        float lengthScale = this.Config.LengthScale / speed;

        if (this._scalesType == typeof(float))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._scalesName,
                new DenseTensor<float>(
                    new[] { this.Config.NoiseScale, lengthScale, this.Config.NoiseW },
                    new[] { 3 }));
        }

        if (this._scalesType == typeof(double))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._scalesName,
                new DenseTensor<double>(
                    new[]
                    {
                        (double)this.Config.NoiseScale,
                        (double)lengthScale,
                        (double)this.Config.NoiseW
                    },
                    new[] { 3 }));
        }

        throw new NotSupportedException(
            $"Piper scales tensor type '{this._scalesType}' is not supported.");
    }

    private NamedOnnxValue CreateSpeakerId(int speakerId)
    {
        if (this._speakerIdName is null || this._speakerIdType is null)
        {
            throw new InvalidOperationException("Piper model does not expose a speaker id input.");
        }

        if (this._speakerIdType == typeof(long))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._speakerIdName,
                new DenseTensor<long>(new[] { (long)Math.Max(0, speakerId) }, new[] { 1 }));
        }

        if (this._speakerIdType == typeof(int))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._speakerIdName,
                new DenseTensor<int>(new[] { Math.Max(0, speakerId) }, new[] { 1 }));
        }

        throw new NotSupportedException(
            $"Piper sid tensor type '{this._speakerIdType}' is not supported.");
    }

    private string FindInputName(params string[] preferredNames)
    {
        foreach (string preferredName in preferredNames)
        {
            if (this._session.InputMetadata.ContainsKey(preferredName))
            {
                return preferredName;
            }
        }

        throw new InvalidOperationException(
            "Piper ONNX model does not expose the expected input. "
            + $"Expected one of: {string.Join(", ", preferredNames)}.");
    }

    private string? FindOptionalInputName(params string[] preferredNames)
    {
        foreach (string preferredName in preferredNames)
        {
            if (this._session.InputMetadata.ContainsKey(preferredName))
            {
                return preferredName;
            }
        }

        return null;
    }

    private static float[] Normalize(float[] audio)
    {
        if (audio.Length == 0)
        {
            return audio;
        }

        float max = 0f;

        foreach (float sample in audio)
        {
            max = Math.Max(max, Math.Abs(sample));
        }

        float divisor = Math.Max(0.01f, max);
        float[] normalized = new float[audio.Length];

        for (int i = 0; i < audio.Length; i++)
        {
            normalized[i] = Math.Clamp(audio[i] / divisor, -1f, 1f);
        }

        return normalized;
    }
}
