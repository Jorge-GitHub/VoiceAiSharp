using VoiceAiSharp.Core.Domain.Constants.Enums;

namespace VoiceAiSharp.Engines.Providers.PiperService.Models;

internal sealed record PiperVoiceSelection(
    TtsLanguage Language,
    TtsVoiceGender Gender,
    TtsVoicePreset Preset,
    string PhonemizerVoice,
    int? SpeakerId);
