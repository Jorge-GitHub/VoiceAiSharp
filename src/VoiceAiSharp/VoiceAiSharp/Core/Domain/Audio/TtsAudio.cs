using VoiceAiSharp.Core.Domain.Constants.Enums;

namespace VoiceAiSharp.Core.Domain.Audio;

public sealed class TtsAudio
{
    public byte[] AudioBytes { get; init; } = [];
    public int SampleRate { get; init; }
    public int Channels { get; init; }
    public TtsAudioFormat Format { get; init; }
    public TimeSpan Duration { get; init; }
    public string Text { get; init; } = "";
    public TtsLanguage Language { get; init; }
    public TtsVoiceGender Gender { get; init; }
    public TtsVoicePreset VoicePreset { get; init; }
    public string ModelName { get; init; } = "";
}
