using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Voices;
using VoiceAiSharp.Engines.Providers.PiperService.Models;
using VoiceAiSharp.Engines.Providers.PiperService.Runtime;

namespace VoiceAiSharp.Engines.Providers.PiperService.Voices;

internal static class PiperVoiceResolver
{
    public static PiperVoiceSelection Resolve(
        TtsVoiceSettings settings,
        PiperModelConfig config)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(config);

        TtsLanguage language = settings.Language == TtsLanguage.Auto
            ? ResolveLanguage(config.LanguageCode)
            : settings.Language;

        TtsVoiceGender gender = settings.Gender == TtsVoiceGender.Any
            ? TtsVoiceGender.Neutral
            : settings.Gender;

        string phonemizerVoice = string.IsNullOrWhiteSpace(config.EspeakVoice)
            ? ResolvePhonemizerVoice(language)
            : config.EspeakVoice;

        return new PiperVoiceSelection(
            language,
            gender,
            settings.Preset,
            phonemizerVoice,
            null);
    }

    private static TtsLanguage ResolveLanguage(string languageCode)
    {
        string normalized = languageCode.Replace('-', '_').ToLowerInvariant();

        if (normalized.StartsWith("en_us", StringComparison.Ordinal))
        {
            return TtsLanguage.AmericanEnglish;
        }

        if (normalized.StartsWith("en_gb", StringComparison.Ordinal))
        {
            return TtsLanguage.BritishEnglish;
        }

        if (normalized.StartsWith("ja", StringComparison.Ordinal))
        {
            return TtsLanguage.Japanese;
        }

        if (normalized.StartsWith("zh", StringComparison.Ordinal)
            || normalized.StartsWith("cmn", StringComparison.Ordinal))
        {
            return TtsLanguage.MandarinChinese;
        }

        if (normalized.StartsWith("es", StringComparison.Ordinal))
        {
            return TtsLanguage.Spanish;
        }

        if (normalized.StartsWith("fr", StringComparison.Ordinal))
        {
            return TtsLanguage.French;
        }

        if (normalized.StartsWith("hi", StringComparison.Ordinal))
        {
            return TtsLanguage.Hindi;
        }

        if (normalized.StartsWith("it", StringComparison.Ordinal))
        {
            return TtsLanguage.Italian;
        }

        if (normalized.StartsWith("pt_br", StringComparison.Ordinal))
        {
            return TtsLanguage.BrazilianPortuguese;
        }

        return TtsLanguage.Auto;
    }

    private static string ResolvePhonemizerVoice(TtsLanguage language)
    {
        return language switch
        {
            TtsLanguage.BritishEnglish => "en-gb",
            TtsLanguage.Japanese => "ja",
            TtsLanguage.MandarinChinese => "cmn",
            TtsLanguage.Spanish => "es",
            TtsLanguage.French => "fr-fr",
            TtsLanguage.Hindi => "hi",
            TtsLanguage.Italian => "it",
            TtsLanguage.BrazilianPortuguese => "pt-br",
            _ => "en-us"
        };
    }
}
