using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Engines;
using Microsoft.ML.OnnxRuntime;

namespace VoiceAiSharp.Engines.Providers.PiperService.Runtime;

internal sealed class PiperSessionOptionsFactory
{
    public SessionOptions Create(TtsRuntimeSettings runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        SessionOptions options = new()
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            IntraOpNumThreads = Math.Max(1, runtime.Threads),
            InterOpNumThreads = 1
        };

        try
        {
            this.ConfigureProvider(options, runtime.Backend);

            return options;
        }
        catch
        {
            options.Dispose();
            throw;
        }
    }

    private void ConfigureProvider(SessionOptions options, TtsBackend backend)
    {
        switch (backend)
        {
            case TtsBackend.Auto:
            case TtsBackend.Cpu:
                return;
            case TtsBackend.Cuda:
                this.AppendCudaProvider(options);
                return;
            case TtsBackend.DirectML:
                this.AppendDirectMlProvider(options);
                return;
            default:
                throw new NotImplementedException(
                    $"TTS backend '{backend}' is not implemented yet.");
        }
    }

    private void AppendCudaProvider(SessionOptions options)
    {
        try
        {
            options.AppendExecutionProvider_CUDA(0);
        }
        catch (Exception exception) when (this.IsProviderLoadException(exception))
        {
            throw this.CreateProviderException(
                TtsBackend.Cuda,
                "Build VoiceAiSharp with /p:TtsOnnxRuntimeProvider=Cuda "
                + "and run on a machine with compatible NVIDIA CUDA runtime dependencies.",
                exception);
        }
    }

    private void AppendDirectMlProvider(SessionOptions options)
    {
        try
        {
            options.EnableMemoryPattern = false;
            options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;
            options.AppendExecutionProvider_DML(0);
        }
        catch (Exception exception) when (this.IsProviderLoadException(exception))
        {
            throw this.CreateProviderException(
                TtsBackend.DirectML,
                "Build VoiceAiSharp with /p:TtsOnnxRuntimeProvider=DirectML "
                + "and run on a Windows machine with a DirectML-capable adapter.",
                exception);
        }
    }

    private bool IsProviderLoadException(Exception exception)
    {
        return exception is DllNotFoundException
            or EntryPointNotFoundException
            or BadImageFormatException
            or OnnxRuntimeException;
    }

    private InvalidOperationException CreateProviderException(
        TtsBackend backend,
        string detail,
        Exception innerException)
    {
        return new InvalidOperationException(
            $"Piper {backend} backend could not be initialized. {detail} "
            + "ONNX Runtime CUDA and DirectML use different native onnxruntime.dll "
            + "provider builds, so only one provider package can be active in a build.",
            innerException);
    }
}
