using VoiceAiSharp.Core.Domain.Settings.Engines;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace VoiceAiSharp.Engines.Providers.KokoroService.Runtime;

internal sealed class KokoroOnnxRuntime : IDisposable
{
    private readonly InferenceSession _session;
    private readonly KokoroVoiceStyleRepository _voiceStyles;
    private readonly string _inputIdsName;
    private readonly string _styleName;
    private readonly string _speedName;
    private readonly Type _inputIdsType;
    private readonly Type _speedType;
    private readonly object _lock = new();

    public string ModelName { get; }

    public KokoroOnnxRuntime(TtsRuntimeSettings runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        string modelPath = KokoroModelResolver.ResolveModelPath(runtime);

        SessionOptions options = new KokoroSessionOptionsFactory().Create(runtime);

        this._session = new InferenceSession(modelPath, options);
        this._voiceStyles = new KokoroVoiceStyleRepository(modelPath, runtime.ModelDirectory);
        this.ModelName = string.IsNullOrWhiteSpace(runtime.ModelName)
            ? Path.GetFileNameWithoutExtension(modelPath)
            : runtime.ModelName;

        this._inputIdsName = this.FindInputName("input_ids", "tokens");
        this._styleName = this.FindInputName("style", "voice");
        this._speedName = this.FindInputName("speed");
        this._inputIdsType = this._session.InputMetadata[this._inputIdsName].ElementType;
        this._speedType = this._session.InputMetadata[this._speedName].ElementType;
    }

    public float[] Synthesize(int[] tokens, string voiceId, float speed)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentException.ThrowIfNullOrWhiteSpace(voiceId);

        if (tokens.Length == 0)
        {
            throw new ArgumentException("Token list cannot be empty.", nameof(tokens));
        }

        if (!float.IsFinite(speed) || speed <= 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(speed),
                "Voice speed must be a finite value greater than zero.");
        }

        float[] style = this._voiceStyles.GetStyleVector(voiceId, tokens.Length);

        List<NamedOnnxValue> inputs =
        [
            this.CreateInputIds(tokens),
            NamedOnnxValue.CreateFromTensor(
                this._styleName,
                new DenseTensor<float>(style, new[] { 1, style.Length })),
            this.CreateSpeed(speed)
        ];

        lock (this._lock)
        {
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results =
                this._session.Run(inputs);

            return results.First().AsTensor<float>().ToArray();
        }
    }

    public void Dispose()
    {
        this._session.Dispose();
    }

    private NamedOnnxValue CreateInputIds(int[] tokens)
    {
        if (this._inputIdsType == typeof(long))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._inputIdsName,
                new DenseTensor<long>(
                    tokens.Select(token => (long)token).ToArray(),
                    new[] { 1, tokens.Length }));
        }

        if (this._inputIdsType == typeof(int))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._inputIdsName,
                new DenseTensor<int>(tokens, new[] { 1, tokens.Length }));
        }

        throw new NotSupportedException(
            $"Kokoro input_ids tensor type '{this._inputIdsType}' is not supported.");
    }

    private NamedOnnxValue CreateSpeed(float speed)
    {
        if (this._speedType == typeof(float))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._speedName,
                new DenseTensor<float>(new[] { speed }, new[] { 1 }));
        }

        if (this._speedType == typeof(double))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._speedName,
                new DenseTensor<double>(new[] { (double)speed }, new[] { 1 }));
        }

        if (this._speedType == typeof(long))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._speedName,
                new DenseTensor<long>(new[] { (long)Math.Round(speed) }, new[] { 1 }));
        }

        if (this._speedType == typeof(int))
        {
            return NamedOnnxValue.CreateFromTensor(
                this._speedName,
                new DenseTensor<int>(new[] { (int)Math.Round(speed) }, new[] { 1 }));
        }

        throw new NotSupportedException(
            $"Kokoro speed tensor type '{this._speedType}' is not supported.");
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
            "Kokoro ONNX model does not expose the expected input. "
            + $"Expected one of: {string.Join(", ", preferredNames)}.");
    }
}
