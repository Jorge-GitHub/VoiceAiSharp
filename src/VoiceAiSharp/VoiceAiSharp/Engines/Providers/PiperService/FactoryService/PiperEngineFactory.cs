using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Engines.Providers.PiperService.Engines;

namespace VoiceAiSharp.Engines.Providers.PiperService.FactoryService;

internal sealed class PiperEngineFactory
{
    public PiperEngine Create(TtsLocalClientSettings settings, TtsBackend backend)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new PiperEngine(settings, backend);
    }
}
