using VoiceAiSharp.Core;
using VoiceAiSharp.Core.Domain.Audio;
using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;

namespace VoiceAiSharp.Ut.Core;

[TestClass]
public sealed class PublicApiTests
{
    [TestMethod]
    public async Task SpeakAsync_WithWhitespaceText_ReturnsEmptyAudio()
    {
        TtsLocalClientSettings settings = new();
        using TtsLocalClient tts = new(settings);

        TtsAudio audio = await tts.SpeakAsync(" ");

        Assert.AreEqual(TimeSpan.Zero, audio.Duration);
        Assert.AreEqual("", audio.Text);
        Assert.AreEqual(settings.Audio.SampleRate, audio.SampleRate);
        Assert.AreEqual(settings.Audio.Channels, audio.Channels);
        Assert.AreEqual(settings.Audio.Format, audio.Format);
    }

    [TestMethod]
    public void TtsAudio_IsProviderAgnostic()
    {
        TtsAudio audio = new()
        {
            AudioBytes = [1, 2, 3],
            SampleRate = 24000,
            Channels = 1,
            Format = TtsAudioFormat.Wav,
            Duration = TimeSpan.FromSeconds(1),
            Text = "Hello George",
            Language = TtsLanguage.AmericanEnglish,
            Gender = TtsVoiceGender.Female,
            VoicePreset = TtsVoicePreset.Default,
            ModelName = "kokoro-v1.0"
        };

        Assert.HasCount(3, audio.AudioBytes);
        Assert.AreEqual("Hello George", audio.Text);
        Assert.AreEqual(TtsAudioFormat.Wav, audio.Format);
    }
}
