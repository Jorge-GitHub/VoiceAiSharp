namespace VoiceAiSharp.Engines.Providers.KokoroService.Text;

internal interface IKokoroTextChunker
{
    IReadOnlyList<string> Chunk(string text);
}
