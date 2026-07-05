using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Voices;
using VoiceAiSharp.Engines.Providers.KokoroService.Models;

namespace VoiceAiSharp.Engines.Providers.KokoroService.Voices;

internal static class KokoroVoicePresetResolver
{
    private static readonly KokoroVoiceMapping[] PresetMappings =
    [
        new(TtsLanguage.AmericanEnglish, TtsVoiceGender.Female, TtsVoicePreset.Heart, "af_heart", "en-us"),
        new(TtsLanguage.AmericanEnglish, TtsVoiceGender.Female, TtsVoicePreset.Bella, "af_bella", "en-us"),
        new(TtsLanguage.AmericanEnglish, TtsVoiceGender.Male, TtsVoicePreset.Michael, "am_michael", "en-us"),
        new(TtsLanguage.BritishEnglish, TtsVoiceGender.Male, TtsVoicePreset.George, "bm_george", "en-gb"),
        new(TtsLanguage.BritishEnglish, TtsVoiceGender.Female, TtsVoicePreset.Emma, "bf_emma", "en-gb"),
    ];

    private static readonly KokoroVoiceMapping[] DefaultMappings =
    [
        new(TtsLanguage.AmericanEnglish, TtsVoiceGender.Female, TtsVoicePreset.Default, "af_heart", "en-us"),
        new(TtsLanguage.AmericanEnglish, TtsVoiceGender.Male, TtsVoicePreset.Default, "am_michael", "en-us"),
        new(TtsLanguage.BritishEnglish, TtsVoiceGender.Female, TtsVoicePreset.Default, "bf_emma", "en-gb"),
        new(TtsLanguage.BritishEnglish, TtsVoiceGender.Male, TtsVoicePreset.Default, "bm_george", "en-gb"),
        new(TtsLanguage.Japanese, TtsVoiceGender.Female, TtsVoicePreset.Default, "jf_alpha", "ja"),
        new(TtsLanguage.Japanese, TtsVoiceGender.Male, TtsVoicePreset.Default, "jm_kumo", "ja"),
        new(TtsLanguage.MandarinChinese, TtsVoiceGender.Female, TtsVoicePreset.Default, "zf_xiaobei", "cmn"),
        new(TtsLanguage.MandarinChinese, TtsVoiceGender.Male, TtsVoicePreset.Default, "zm_yunjian", "cmn"),
        new(TtsLanguage.Spanish, TtsVoiceGender.Female, TtsVoicePreset.Default, "ef_dora", "es"),
        new(TtsLanguage.Spanish, TtsVoiceGender.Male, TtsVoicePreset.Default, "em_alex", "es"),
        new(TtsLanguage.French, TtsVoiceGender.Female, TtsVoicePreset.Default, "ff_siwis", "fr-fr"),
        new(TtsLanguage.Hindi, TtsVoiceGender.Female, TtsVoicePreset.Default, "hf_alpha", "hi"),
        new(TtsLanguage.Hindi, TtsVoiceGender.Male, TtsVoicePreset.Default, "hm_omega", "hi"),
        new(TtsLanguage.Italian, TtsVoiceGender.Female, TtsVoicePreset.Default, "if_sara", "it"),
        new(TtsLanguage.Italian, TtsVoiceGender.Male, TtsVoicePreset.Default, "im_nicola", "it"),
        new(TtsLanguage.BrazilianPortuguese, TtsVoiceGender.Female, TtsVoicePreset.Default, "pf_dora", "pt-br"),
        new(TtsLanguage.BrazilianPortuguese, TtsVoiceGender.Male, TtsVoicePreset.Default, "pm_alex", "pt-br"),
    ];

    public static KokoroVoiceSelection Resolve(TtsVoiceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.Preset == TtsVoicePreset.Default
            ? ResolveDefault(settings)
            : ResolvePreset(settings);
    }

    private static KokoroVoiceSelection ResolvePreset(TtsVoiceSettings settings)
    {
        TtsLanguage language = ResolvePresetLanguage(settings.Language, settings.Preset);

        KokoroVoiceMapping? mapping = PresetMappings.FirstOrDefault(voice =>
            voice.Language == language
            && voice.Preset == settings.Preset
            && MatchesGender(voice.Gender, settings.Gender));

        if (mapping is null)
        {
            throw new InvalidOperationException(
                "No Kokoro voice mapping exists for VoiceAiSharp voice preset "
                + $"'{settings.Preset}' with language '{language}' and gender '{settings.Gender}'.");
        }

        return mapping.ToSelection(settings.Preset);
    }

    private static KokoroVoiceSelection ResolveDefault(TtsVoiceSettings settings)
    {
        TtsLanguage language = settings.Language == TtsLanguage.Auto
            ? TtsLanguage.AmericanEnglish
            : settings.Language;

        KokoroVoiceMapping? mapping = DefaultMappings.FirstOrDefault(voice =>
            voice.Language == language
            && MatchesGender(voice.Gender, settings.Gender));

        if (mapping is null)
        {
            throw new InvalidOperationException(
                "No Kokoro default voice mapping exists for VoiceAiSharp language "
                + $"'{language}' and gender '{settings.Gender}'.");
        }

        return mapping.ToSelection(TtsVoicePreset.Default);
    }

    private static TtsLanguage ResolvePresetLanguage(
        TtsLanguage language,
        TtsVoicePreset preset)
    {
        if (language != TtsLanguage.Auto)
        {
            return language;
        }

        return preset switch
        {
            TtsVoicePreset.George or TtsVoicePreset.Emma => TtsLanguage.BritishEnglish,
            TtsVoicePreset.Heart
                or TtsVoicePreset.Bella
                or TtsVoicePreset.Michael => TtsLanguage.AmericanEnglish,
            _ => TtsLanguage.AmericanEnglish
        };
    }

    private static bool MatchesGender(
        TtsVoiceGender voiceGender,
        TtsVoiceGender requestedGender)
    {
        return requestedGender is TtsVoiceGender.Any or TtsVoiceGender.Neutral
            || voiceGender == requestedGender;
    }

    private sealed record KokoroVoiceMapping(
        TtsLanguage Language,
        TtsVoiceGender Gender,
        TtsVoicePreset Preset,
        string VoiceId,
        string PhonemizerVoice)
    {
        public KokoroVoiceSelection ToSelection(TtsVoicePreset preset)
            => new(this.VoiceId, this.Language, this.Gender, preset, this.PhonemizerVoice);
    }
}
