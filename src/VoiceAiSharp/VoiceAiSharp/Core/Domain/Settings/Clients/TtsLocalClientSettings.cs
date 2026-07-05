using VoiceAiSharp.Core.Domain.Settings.Audio;
using VoiceAiSharp.Core.Domain.Settings.Engines;
using VoiceAiSharp.Core.Domain.Settings.Voices;

namespace VoiceAiSharp.Core.Domain.Settings.Clients;

public sealed class TtsLocalClientSettings
{
    public TtsRuntimeSettings Runtime { get; set; } = new();
    public TtsVoiceSettings Voice { get; set; } = new();
    public TtsAudioSettings Audio { get; set; } = new();
}
