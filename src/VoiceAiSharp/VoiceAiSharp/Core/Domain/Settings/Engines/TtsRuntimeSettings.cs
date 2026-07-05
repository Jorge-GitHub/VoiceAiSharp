using VoiceAiSharp.Core.Domain.Constants.Enums;

namespace VoiceAiSharp.Core.Domain.Settings.Engines;

public sealed class TtsRuntimeSettings
{
    public TtsEngineType Engine { get; set; } = TtsEngineType.Auto;
    public TtsBackend Backend { get; set; } = TtsBackend.Auto;
    public string ModelPath { get; set; } = "";
    public string ModelDirectory { get; set; } = "";
    public string ModelName { get; set; } = "";
    public int Threads { get; set; } = Math.Max(1, Environment.ProcessorCount / 2);
}
