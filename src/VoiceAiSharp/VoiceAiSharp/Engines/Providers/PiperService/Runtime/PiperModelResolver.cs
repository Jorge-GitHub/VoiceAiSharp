using VoiceAiSharp.Core.Domain.Settings.Engines;
using System.Text;

namespace VoiceAiSharp.Engines.Providers.PiperService.Runtime;

internal static class PiperModelResolver
{
    private static readonly string[] KnownModelNames =
    [
        "piper.onnx",
        "model.onnx",
        "en_US-amy-low.onnx",
        "en_US-amy-medium.onnx",
        "en_US-lessac-medium.onnx",
        "en_GB-alan-medium.onnx"
    ];

    public static PiperModelFiles Resolve(TtsRuntimeSettings runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        string modelPath = ResolveModelPath(runtime);
        string configPath = ResolveConfigPath(modelPath);

        return new PiperModelFiles(modelPath, configPath);
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
            if (Directory.Exists(models))
            {
                directories.Add(Path.GetFullPath(models));
            }
        }

        return directories.ToArray();
    }

    private static string ResolveModelPath(TtsRuntimeSettings runtime)
    {
        if (!string.IsNullOrWhiteSpace(runtime.ModelPath))
        {
            string fullPath = Path.GetFullPath(runtime.ModelPath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    $"Piper model file was not found: {fullPath}",
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
                if (File.Exists(candidate) && HasConfig(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }

            foreach (string candidate in EnumerateConfiguredModels(directory))
            {
                return Path.GetFullPath(candidate);
            }
        }

        throw new FileNotFoundException(CreateMissingModelMessage(runtime, directories));
    }

    private static string ResolveConfigPath(string modelPath)
    {
        string onnxJson = modelPath + ".json";

        if (File.Exists(onnxJson))
        {
            return Path.GetFullPath(onnxJson);
        }

        string plainJson = Path.ChangeExtension(modelPath, ".json");

        if (File.Exists(plainJson))
        {
            return Path.GetFullPath(plainJson);
        }

        throw new FileNotFoundException(
            "Piper model config file was not found. Expected either "
            + $"'{onnxJson}' or '{plainJson}'.");
    }

    private static bool HasConfig(string modelPath)
    {
        return File.Exists(modelPath + ".json")
            || File.Exists(Path.ChangeExtension(modelPath, ".json"));
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

    private static IEnumerable<string> EnumerateConfiguredModels(string directory)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (string candidate in Directory.EnumerateFiles(directory, "*.onnx")
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (HasConfig(candidate))
            {
                yield return candidate;
            }
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
            "Piper model file was not found. Set TtsRuntimeSettings.ModelPath "
            + "to a Piper .onnx file with a matching .onnx.json config, "
            + "or place both files in a models/ folder.");

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
