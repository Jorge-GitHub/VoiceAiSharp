using VoiceAiSharp.Core;
using VoiceAiSharp.Core.Domain.Audio;
using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Extensions.Core.Domain.Audio;
using VoiceAiSharp.Ut.Helpers;

namespace VoiceAiSharp.Ut.Core;

[TestClass]
[DoNotParallelize]
public sealed class TtsAudioExtensionsTests
{
    [TestMethod]
    public async Task SaveAndWriteToAsync_WithSpeakAsyncAudio_WritesAudioBytes()
    {
        TtsAudio audio = await this.CreateSpeakAsyncAudio();
        string filePath = this.GetAudioOutputFilePath(
            "save-and-write-to-speak-async.wav");

        await audio.SaveAsync(filePath);

        byte[] bytes = await File.ReadAllBytesAsync(filePath);
        CollectionAssert.AreEqual(audio.AudioBytes, bytes);
        this.AssertWav(bytes);

        using MemoryStream stream = new();
        await audio.WriteToAsync(stream);

        CollectionAssert.AreEqual(audio.AudioBytes, stream.ToArray());
    }

    [TestMethod]
    public async Task TrySaveAndTryWriteToAsync_WithSpeakAsyncAudio_ReturnsTrue()
    {
        TtsAudio audio = await this.CreateSpeakAsyncAudio();
        string filePath = this.GetAudioOutputFilePath(
            "try-save-and-try-write-to-speak-async.wav");

        bool saved = await audio.TrySaveAsync(filePath);

        byte[] bytes = await File.ReadAllBytesAsync(filePath);
        Assert.IsTrue(saved);
        CollectionAssert.AreEqual(audio.AudioBytes, bytes);
        this.AssertWav(bytes);

        using MemoryStream stream = new();
        bool written = await audio.TryWriteToAsync(stream);

        Assert.IsTrue(written);
        CollectionAssert.AreEqual(audio.AudioBytes, stream.ToArray());
    }

    [TestMethod]
    public async Task TrySaveAsync_WithMissingDirectory_ReturnsFalse()
    {
        TtsAudio audio = this.CreateFakeAudio();
        string filePath = Path.Combine(
            this.GetAudioOutputDirectory(),
            Guid.NewGuid().ToString("N"),
            "voice.wav");

        bool saved = await audio.TrySaveAsync(filePath);

        Assert.IsFalse(saved);
        Assert.IsFalse(File.Exists(filePath));
    }

    [TestMethod]
    public async Task TrySaveAsync_WithNullAudio_ReturnsFalse()
    {
        TtsAudio? audio = null;
        string filePath = this.GetAudioOutputFilePath(
            "null-audio.wav");

        bool saved = await audio.TrySaveAsync(filePath);

        Assert.IsFalse(saved);
    }

    [TestMethod]
    public async Task TryWriteToAsync_WithNullStream_ReturnsFalse()
    {
        TtsAudio audio = this.CreateFakeAudio();

        bool written = await audio.TryWriteToAsync(null);

        Assert.IsFalse(written);
    }

    private async Task<TtsAudio> CreateSpeakAsyncAudio()
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

        return await tts.SpeakAsync("Hello George");
    }

    private TtsAudio CreateFakeAudio()
    {
        return new TtsAudio
        {
            AudioBytes = [82, 73, 70, 70],
            SampleRate = 24000,
            Channels = 1,
            Format = TtsAudioFormat.Wav,
            Duration = TimeSpan.FromMilliseconds(1),
            Text = "Hello George",
            Language = TtsLanguage.BritishEnglish,
            Gender = TtsVoiceGender.Male,
            VoicePreset = TtsVoicePreset.George,
            ModelName = "kokoro"
        };
    }

    private void AssertWav(byte[] bytes)
    {
        Assert.IsGreaterThan(44, bytes.Length);
        Assert.AreEqual((byte)'R', bytes[0]);
        Assert.AreEqual((byte)'I', bytes[1]);
        Assert.AreEqual((byte)'F', bytes[2]);
        Assert.AreEqual((byte)'F', bytes[3]);
        Assert.AreEqual((byte)'W', bytes[8]);
        Assert.AreEqual((byte)'A', bytes[9]);
        Assert.AreEqual((byte)'V', bytes[10]);
        Assert.AreEqual((byte)'E', bytes[11]);
    }

    private string GetAudioOutputFilePath(string fileName)
    {
        return Path.Combine(this.GetAudioOutputDirectory(), fileName);
    }

    private string GetAudioOutputDirectory()
    {
        string outputDirectory = Path.Combine(
            this.GetRepositoryRootDirectory(),
            "TestResults",
            "Audio");

        Directory.CreateDirectory(outputDirectory);

        return outputDirectory;
    }

    private string GetRepositoryRootDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        string? solutionDirectory = null;

        while (directory is not null)
        {
            string gitDirectory = Path.Combine(directory.FullName, ".git");

            if (Directory.Exists(gitDirectory))
            {
                return directory.FullName;
            }

            string solutionPath = Path.Combine(
                directory.FullName,
                "VoiceAiSharp.slnx");

            if (solutionDirectory is null && File.Exists(solutionPath))
            {
                solutionDirectory = directory.FullName;
            }

            directory = directory.Parent;
        }

        return solutionDirectory ?? AppContext.BaseDirectory;
    }
}
