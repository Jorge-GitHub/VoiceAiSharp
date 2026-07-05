using VoiceAiSharp.Core.Domain.Settings.Engines;
using System.Text;

namespace VoiceAiSharp.Engines.Providers.KokoroService.Runtime;

internal static class KokoroModelResolver
{
    private static readonly string[] KnownModelNames =
    [
        "kokoro-v1.0.onnx",
        "kokoro-v1.0.fp16.onnx",
        "kokoro-v0_19.onnx",
        "model.onnx"
    ];

    public static string ResolveModelPath(TtsRuntimeSettings runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        if (!string.IsNullOrWhiteSpace(runtime.ModelPath))
        {
            string fullPath = Path.GetFullPath(runtime.ModelPath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    $"Kokoro model file was not found: {fullPath}",
                    fullPath);
            }

            return fullPath;
        }

        IReadOnlyList<string> directories = ResolveModelDirectories(runtime.ModelDirectory);
        foreach (string directory in directories)
        {
            foreach (string fileName in EnumerateCandidateModelNames(runtime.ModelName))
            {
                string candidate = Path.Combine(directory, fileName);
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }
        }

        throw new FileNotFoundException(CreateMissingModelMessage(runtime, directories));
    }

    public static IReadOnlyList<string> ResolveModelDirectories(string modelDirectory)
    {
        HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(modelDirectory))
        {
            directories.Add(Path.GetFullPath(modelDirectory));
        }

        foreach (string root in EnumerateSearchRoots())
        {
            string models = Path.Combine(root, "models");
            AddDirectoryIfExists(directories, models);

            foreach (string libraryDirectory in EnumerateProviderLibraryDirectories(root))
            {
                AddDirectoryIfExists(directories, libraryDirectory);
            }
        }

        return directories.ToArray();
    }

    private static void AddDirectoryIfExists(HashSet<string> directories, string directory)
    {
        if (Directory.Exists(directory))
        {
            directories.Add(Path.GetFullPath(directory));
        }
    }

    private static IEnumerable<string> EnumerateProviderLibraryDirectories(string root)
    {
        yield return Path.Combine(
            root,
            "Engines",
            "Providers",
            "KokoroService",
            "Libraries");

        yield return Path.Combine(
            root,
            "Engines",
            "Providers",
            "KokoroService",
            "Libraries",
            "Native");
    }

    private static IEnumerable<string> EnumerateCandidateModelNames(string modelName)
    {
        if (!string.IsNullOrWhiteSpace(modelName))
        {
            yield return modelName;

            if (Path.GetExtension(modelName).Length == 0)
            {
                yield return modelName + ".onnx";
            }

            yield break;
        }

        foreach (string knownName in KnownModelNames)
        {
            yield return knownName;
        }
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);

        foreach (string start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            DirectoryInfo? directory = new(start);

            while (directory is not null && visited.Add(directory.FullName))
            {
                yield return directory.FullName;
                directory = directory.Parent;
            }
        }
    }

    private static string CreateMissingModelMessage(
        TtsRuntimeSettings runtime,
        IReadOnlyList<string> directories)
    {
        StringBuilder message = new();

        message.Append(
            "Kokoro model file was not found. Set TtsRuntimeSettings.ModelPath, "
            + "or place a Kokoro ONNX model in a models/ folder.");

        if (!string.IsNullOrWhiteSpace(runtime.ModelName))
        {
            message.Append($" Requested model name: '{runtime.ModelName}'.");
        }

        if (directories.Count > 0)
        {
            message.Append(" Searched: ");
            message.Append(string.Join(", ", directories));
        }

        return message.ToString();
    }
}
