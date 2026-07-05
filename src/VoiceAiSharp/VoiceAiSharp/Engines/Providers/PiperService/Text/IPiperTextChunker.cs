namespace VoiceAiSharp.Engines.Providers.PiperService.Text;

internal interface IPiperTextChunker
{
    IReadOnlyList<string> Chunk(string text);
}
