using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Core.Engines.Base;
using VoiceAiSharp.Engines.Providers.KokoroService.Engines;

namespace VoiceAiSharp.Engines.Providers.KokoroService.FactoryService;

internal sealed class KokoroEngineFactory
{
    public TtsEngineBase Create(TtsLocalClientSettings settings, TtsBackend backend)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new KokoroEngine(settings, backend);
    }
}
