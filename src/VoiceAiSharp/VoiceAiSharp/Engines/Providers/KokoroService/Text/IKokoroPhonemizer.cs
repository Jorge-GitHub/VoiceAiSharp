using VoiceAiSharp.Engines.Providers.KokoroService.Models;

namespace VoiceAiSharp.Engines.Providers.KokoroService.Text;

internal interface IKokoroPhonemizer
{
    Task<string> PhonemizeAsync(
        string text,
        KokoroVoiceSelection voice,
        CancellationToken cancellationToken);
}
