using System.IO.Compression;

namespace VoiceAiSharp.Engines.Providers.KokoroService.Runtime;

internal sealed class KokoroVoiceStyleRepository
{
    private static readonly string[] ArchiveNames =
    [
        "voices-v1.0.bin",
        "voices-v1.0.npz",
        "voices.bin",
        "voices.npz"
    ];

    private readonly IReadOnlyList<string> _directories;
    private readonly Dictionary<string, FloatArrayData> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public KokoroVoiceStyleRepository(string modelPath, string modelDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);

        HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);

        string? modelFolder = Path.GetDirectoryName(modelPath);
        if (!string.IsNullOrWhiteSpace(modelFolder))
        {
            directories.Add(modelFolder);
        }

        foreach (string directory in KokoroModelResolver.ResolveModelDirectories(modelDirectory))
        {
            directories.Add(directory);
        }

        this._directories = directories.ToArray();
    }

    public float[] GetStyleVector(string voiceId, int tokenCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(voiceId);

        lock (this._lock)
        {
            if (!this._cache.TryGetValue(voiceId, out FloatArrayData? data))
            {
                data = this.LoadVoice(voiceId);
                this._cache[voiceId] = data;
            }

            return this.SelectStyleVector(data, tokenCount);
        }
    }

    private FloatArrayData LoadVoice(string voiceId)
    {
        foreach (string filePath in this.EnumerateVoiceFiles(voiceId))
        {
            if (!File.Exists(filePath))
            {
                continue;
            }

            using FileStream stream = File.OpenRead(filePath);

            if (Path.GetExtension(filePath).Equals(".npy", StringComparison.OrdinalIgnoreCase))
            {
                return NumpyArrayReader.ReadSingleArray(stream);
            }

            return NumpyArrayReader.ReadRawSingleArray(stream);
        }

        foreach (string archivePath in this.EnumerateVoiceArchives())
        {
            if (!File.Exists(archivePath) || !this.LooksLikeZipFile(archivePath))
            {
                continue;
            }

            FloatArrayData? data = this.TryLoadFromArchive(archivePath, voiceId);
            if (data is not null)
            {
                return data;
            }
        }

        throw new FileNotFoundException(
            $"Kokoro voice style vector for '{voiceId}' was not found. "
            + "Place voice files next to the model, under models/voices, "
            + "or provide a voices-v1.0.bin archive.");
    }

    private IEnumerable<string> EnumerateVoiceFiles(string voiceId)
    {
        foreach (string directory in this._directories)
        {
            yield return Path.Combine(directory, "voices", voiceId + ".bin");
            yield return Path.Combine(directory, "voices", voiceId + ".npy");
            yield return Path.Combine(directory, voiceId + ".bin");
            yield return Path.Combine(directory, voiceId + ".npy");
        }
    }

    private IEnumerable<string> EnumerateVoiceArchives()
    {
        foreach (string directory in this._directories)
        {
            foreach (string archiveName in ArchiveNames)
            {
                yield return Path.Combine(directory, archiveName);
                yield return Path.Combine(directory, "voices", archiveName);
            }
        }
    }

    private FloatArrayData? TryLoadFromArchive(string archivePath, string voiceId)
    {
        using ZipArchive archive = ZipFile.OpenRead(archivePath);

        ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(item =>
        {
            string name = item.FullName.Replace('\\', '/');
            string fileName = Path.GetFileName(name);

            return name.Equals(voiceId, StringComparison.OrdinalIgnoreCase)
                || name.Equals(voiceId + ".npy", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals(voiceId, StringComparison.OrdinalIgnoreCase)
                || fileName.Equals(voiceId + ".npy", StringComparison.OrdinalIgnoreCase);
        });

        if (entry is null)
        {
            return null;
        }

        using Stream entryStream = entry.Open();
        using MemoryStream memory = new();
        entryStream.CopyTo(memory);
        memory.Position = 0;

        return NumpyArrayReader.ReadSingleArray(memory);
    }

    private float[] SelectStyleVector(FloatArrayData data, int tokenCount)
    {
        if (data.Values.Length == 0)
        {
            throw new InvalidDataException("Kokoro voice style vector is empty.");
        }

        int rowSize = this.ResolveRowSize(data);
        int rowCount = Math.Max(1, data.Values.Length / rowSize);
        int row = Math.Clamp(tokenCount, 0, rowCount - 1);

        float[] vector = new float[rowSize];
        Array.Copy(data.Values, row * rowSize, vector, 0, rowSize);
        return vector;
    }

    private int ResolveRowSize(FloatArrayData data)
    {
        if (data.Shape.Length > 0 && data.Shape[^1] > 0)
        {
            return data.Shape[^1];
        }

        if (data.Values.Length % 256 == 0)
        {
            return 256;
        }

        if (data.Values.Length % 128 == 0)
        {
            return 128;
        }

        return data.Values.Length;
    }

    private bool LooksLikeZipFile(string path)
    {
        Span<byte> signature = stackalloc byte[2];

        using FileStream stream = File.OpenRead(path);
        if (stream.Read(signature) != signature.Length)
        {
            return false;
        }

        return signature[0] == 'P' && signature[1] == 'K';
    }
}
