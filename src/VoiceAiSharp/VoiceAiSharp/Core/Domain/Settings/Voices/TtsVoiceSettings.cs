using VoiceAiSharp.Core.Domain.Constants.Enums;

namespace VoiceAiSharp.Core.Domain.Settings.Voices;

public sealed class TtsVoiceSettings
{
    public TtsLanguage Language { get; set; } = TtsLanguage.AmericanEnglish;
    public TtsVoiceGender Gender { get; set; } = TtsVoiceGender.Female;
    public TtsVoicePreset Preset { get; set; } = TtsVoicePreset.Default;
    public float Speed { get; set; } = 1.0f;
}
