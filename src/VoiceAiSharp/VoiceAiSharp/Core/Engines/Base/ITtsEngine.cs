using VoiceAiSharp.Core.Domain.Audio;
using VoiceAiSharp.Core.Domain.Settings.Clients;

namespace VoiceAiSharp.Core.Engines.Base;

internal interface ITtsEngine : IDisposable
{
    Task<TtsAudio> SpeakAsync(
        string text,
        TtsLocalClientSettings settings,
        CancellationToken cancellationToken);
}
