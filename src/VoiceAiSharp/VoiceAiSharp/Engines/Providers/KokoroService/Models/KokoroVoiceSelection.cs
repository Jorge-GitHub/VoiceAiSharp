using VoiceAiSharp.Core.Domain.Constants.Enums;

namespace VoiceAiSharp.Engines.Providers.KokoroService.Models;

internal sealed record KokoroVoiceSelection(
    string VoiceId,
    TtsLanguage Language,
    TtsVoiceGender Gender,
    TtsVoicePreset Preset,
    string PhonemizerVoice);
