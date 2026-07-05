using VoiceAiSharp.Engines.Providers.PiperService.Models;

namespace VoiceAiSharp.Engines.Providers.PiperService.Text;

internal interface IPiperPhonemizer
{
    Task<string> PhonemizeAsync(
        string text,
        PiperVoiceSelection voice,
        CancellationToken cancellationToken);
}
