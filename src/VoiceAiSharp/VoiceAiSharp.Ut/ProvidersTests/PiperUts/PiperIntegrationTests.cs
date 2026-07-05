using VoiceAiSharp.Core;
using VoiceAiSharp.Core.Domain.Audio;
using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Ut.Helpers;

namespace VoiceAiSharp.Ut.ProvidersTests.PiperUts;

[TestClass]
[DoNotParallelize]
public sealed class PiperIntegrationTests
{
    [TestMethod]
    public async Task SpeakAsync_WithPiperModel_ReturnsAudio()
    {
        string modelPath = this.ResolveModelPath();

        this.EnsurePiperConfigExists(modelPath);

        TtsLocalClientSettings settings = new();

        settings.Runtime.ModelPath = modelPath;
        settings.Runtime.ModelDirectory = Path.GetDirectoryName(modelPath) ?? "";
        settings.Runtime.Engine = TtsEngineType.Piper;
        settings.Runtime.Backend = TtsBackend.Cpu;
        settings.Runtime.ModelName = Path.GetFileNameWithoutExtension(modelPath);
        settings.Voice.Language = TtsLanguage.Auto;
        settings.Voice.Gender = TtsVoiceGender.Any;
        settings.Audio.Format = TtsAudioFormat.Wav;

        using TtsLocalClient tts = new(settings);

        TtsAudio audio = await tts.SpeakAsync("Hello from Piper.");

        Assert.IsGreaterThan(44, audio.AudioBytes.Length);
        Assert.AreEqual(TtsAudioFormat.Wav, audio.Format);
        Assert.AreEqual(settings.Audio.SampleRate, audio.SampleRate);
        Assert.AreEqual(1, audio.Channels);
        Assert.IsGreaterThan(TimeSpan.Zero, audio.Duration);
        Assert.AreEqual(settings.Runtime.ModelName, audio.ModelName);
    }

    private string ResolveModelPath()
    {
        string? configuredModelPath = Environment.GetEnvironmentVariable(
            "VOICEAISHARP_TTS_PIPER_MODEL_PATH");

        if (!string.IsNullOrWhiteSpace(configuredModelPath))
        {
            return configuredModelPath;
        }

        return TestModelResolver.GetFirstExistingModelPathFor(
            "Piper",
            "en_US-amy-low.onnx",
            "en_US-amy-medium.onnx",
            "en_US-lessac-medium.onnx",
            "model.onnx");
    }

    private void EnsurePiperConfigExists(string modelPath)
    {
        if (!File.Exists(modelPath + ".json")
            && !File.Exists(Path.ChangeExtension(modelPath, ".json")))
        {
            Assert.Inconclusive(
                "Piper model config file not found. Place the matching .onnx.json "
                + "beside the Piper .onnx model to run this integration test.");
        }
    }
}
