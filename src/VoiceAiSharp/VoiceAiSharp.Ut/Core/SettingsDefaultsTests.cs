using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;

namespace VoiceAiSharp.Ut.Core;

[TestClass]
public sealed class SettingsDefaultsTests
{
    [TestMethod]
    public void Defaults_AreProviderAgnosticAndUsable()
    {
        TtsLocalClientSettings settings = new();

        Assert.AreEqual(TtsEngineType.Auto, settings.Runtime.Engine);
        Assert.AreEqual(TtsBackend.Auto, settings.Runtime.Backend);
        Assert.AreEqual("", settings.Runtime.ModelPath);
        Assert.AreEqual("", settings.Runtime.ModelDirectory);
        Assert.AreEqual("", settings.Runtime.ModelName);
        Assert.IsGreaterThanOrEqualTo(1, settings.Runtime.Threads);

        Assert.AreEqual(TtsLanguage.AmericanEnglish, settings.Voice.Language);
        Assert.AreEqual(TtsVoiceGender.Female, settings.Voice.Gender);
        Assert.AreEqual(TtsVoicePreset.Default, settings.Voice.Preset);
        Assert.AreEqual(1.0f, settings.Voice.Speed);

        Assert.AreEqual(TtsAudioFormat.Wav, settings.Audio.Format);
        Assert.AreEqual(24000, settings.Audio.SampleRate);
        Assert.AreEqual(1, settings.Audio.Channels);
    }
}
