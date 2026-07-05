using VoiceAiSharp.Core.Domain.Constants.Enums;

namespace VoiceAiSharp.Core.Domain.Settings.Audio;

public sealed class TtsAudioSettings
{
    public TtsAudioFormat Format { get; set; } = TtsAudioFormat.Wav;
    public int SampleRate { get; set; } = 24000;
    public int Channels { get; set; } = 1;
}
