using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Voices;
using VoiceAiSharp.Engines.Providers.KokoroService.Models;
using VoiceAiSharp.Engines.Providers.KokoroService.Voices;

namespace VoiceAiSharp.Ut.ProvidersTests.KokoroUts;

[TestClass]
public sealed class KokoroVoicePresetResolverTests
{
    [TestMethod]
    public void Resolve_George_MapsToBritishMaleKokoroVoice()
    {
        KokoroVoiceSelection voice = KokoroVoicePresetResolver.Resolve(new TtsVoiceSettings
        {
            Language = TtsLanguage.BritishEnglish,
            Gender = TtsVoiceGender.Male,
            Preset = TtsVoicePreset.George
        });

        Assert.AreEqual("bm_george", voice.VoiceId);
        Assert.AreEqual(TtsLanguage.BritishEnglish, voice.Language);
        Assert.AreEqual(TtsVoiceGender.Male, voice.Gender);
    }

    [TestMethod]
    public void Resolve_Default_IsDeterministicForLanguageAndGender()
    {
        KokoroVoiceSelection voice = KokoroVoicePresetResolver.Resolve(new TtsVoiceSettings
        {
            Language = TtsLanguage.AmericanEnglish,
            Gender = TtsVoiceGender.Female,
            Preset = TtsVoicePreset.Default
        });

        Assert.AreEqual("af_heart", voice.VoiceId);
        Assert.AreEqual(TtsVoicePreset.Default, voice.Preset);
    }

    [TestMethod]
    public void Resolve_MissingPresetMapping_ThrowsClearException()
    {
        try
        {
            KokoroVoicePresetResolver.Resolve(new TtsVoiceSettings
            {
                Language = TtsLanguage.Japanese,
                Gender = TtsVoiceGender.Male,
                Preset = TtsVoicePreset.George
            });

            Assert.Fail("Missing mappings should throw.");
        }
        catch (InvalidOperationException exception)
        {
            StringAssert.Contains(exception.Message, "No Kokoro voice mapping exists");
        }
    }
}
