using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Voices;
using VoiceAiSharp.Engines.Providers.PiperService.Models;
using VoiceAiSharp.Engines.Providers.PiperService.Runtime;
using VoiceAiSharp.Engines.Providers.PiperService.Voices;

namespace VoiceAiSharp.Ut.ProvidersTests.PiperUts;

[TestClass]
public sealed class PiperVoiceResolverTests
{
    [TestMethod]
    public void Resolve_WithAutoLanguage_UsesPiperConfigLanguage()
    {
        PiperModelConfig config = this.CreateConfig();

        PiperVoiceSelection voice = PiperVoiceResolver.Resolve(
            new TtsVoiceSettings
            {
                Language = TtsLanguage.Auto,
                Gender = TtsVoiceGender.Any
            },
            config);

        Assert.AreEqual(TtsLanguage.AmericanEnglish, voice.Language);
        Assert.AreEqual(TtsVoiceGender.Neutral, voice.Gender);
        Assert.AreEqual("en-us", voice.PhonemizerVoice);
    }

    [TestMethod]
    public void Resolve_WithExplicitLanguage_PreservesRequestedLanguage()
    {
        PiperModelConfig config = this.CreateConfig();

        PiperVoiceSelection voice = PiperVoiceResolver.Resolve(
            new TtsVoiceSettings
            {
                Language = TtsLanguage.BritishEnglish,
                Gender = TtsVoiceGender.Male,
                Preset = TtsVoicePreset.George
            },
            config);

        Assert.AreEqual(TtsLanguage.BritishEnglish, voice.Language);
        Assert.AreEqual(TtsVoiceGender.Male, voice.Gender);
        Assert.AreEqual(TtsVoicePreset.George, voice.Preset);
    }

    private PiperModelConfig CreateConfig()
    {
        PiperModelConfigTests helper = new();
        string configPath = helper.WriteConfig(PiperModelConfigTests.CreateConfigJson());

        return PiperModelConfig.Load(configPath);
    }
}
