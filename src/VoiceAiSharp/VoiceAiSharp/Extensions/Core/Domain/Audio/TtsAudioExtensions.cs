using VoiceAiSharp.Core.Domain.Audio;

namespace VoiceAiSharp.Extensions.Core.Domain.Audio;

public static class TtsAudioExtensions
{
    public static Task SaveAsync(this TtsAudio audio,
        string filePath, CancellationToken cancellationToken = default)
    {
        return File.WriteAllBytesAsync(filePath, audio.AudioBytes,
            cancellationToken);
    }

    public static Task WriteToAsync(this TtsAudio audio,
        Stream stream, CancellationToken cancellationToken = default)
    {
        return stream.WriteAsync(audio.AudioBytes,
            cancellationToken).AsTask();
    }

    public static async Task<bool> TrySaveAsync(this TtsAudio? audio,
        string? filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (audio is not null && !string.IsNullOrWhiteSpace(filePath))
            {
                await audio.SaveAsync(filePath, cancellationToken);

                return true;
            }            
        }
        catch { }

        return false;
    }

    public static async Task<bool> TryWriteToAsync(this TtsAudio? audio,
        Stream? stream, CancellationToken cancellationToken = default)
    {
        try
        {
            if (audio is not null && stream is not null && stream.CanWrite)
            {
                await audio.WriteToAsync(stream, cancellationToken);

                return true;
            }
        }
        catch { }

        return false;
    }
}
