using VoiceAiSharp.Core;
using VoiceAiSharp.Core.Domain.Audio;
using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Ut.Helpers;

namespace VoiceAiSharp.Ut.ProvidersTests.KokoroUts;

[TestClass]
[DoNotParallelize]
public sealed class KokoroIntegrationTests
{
    [TestMethod]
    public async Task SpeakAsync_WithKokoroModel_ReturnsAudio()
    {
        string modelPath = TestModelResolver.GetFirstExistingModelPath(
            "kokoro-v1.0.onnx",
            "kokoro-v1.0.fp16.onnx",
            "kokoro-v0_19.onnx",
            "model.onnx");

        TtsLocalClientSettings settings = new();

        settings.Runtime.ModelPath = modelPath;
        settings.Runtime.ModelDirectory = Path.GetDirectoryName(modelPath) ?? "";
        settings.Runtime.Engine = TtsEngineType.Kokoro;
        settings.Runtime.Backend = TtsBackend.Cpu;
        settings.Runtime.ModelName = Path.GetFileNameWithoutExtension(modelPath);
        settings.Voice.Language = TtsLanguage.BritishEnglish;
        settings.Voice.Gender = TtsVoiceGender.Male;
        settings.Voice.Preset = TtsVoicePreset.George;
        settings.Audio.Format = TtsAudioFormat.Wav;

        using TtsLocalClient tts = new(settings);

        TtsAudio audio = await tts.SpeakAsync("Hello George");

        Assert.IsGreaterThan(44, audio.AudioBytes.Length);
        Assert.AreEqual(TtsAudioFormat.Wav, audio.Format);
        Assert.AreEqual(24000, audio.SampleRate);
        Assert.AreEqual(1, audio.Channels);
        Assert.IsGreaterThan(TimeSpan.Zero, audio.Duration);
        Assert.AreEqual(TtsLanguage.BritishEnglish, audio.Language);
        Assert.AreEqual(TtsVoiceGender.Male, audio.Gender);
    }
}
