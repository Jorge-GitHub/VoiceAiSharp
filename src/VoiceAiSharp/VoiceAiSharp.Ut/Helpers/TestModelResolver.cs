namespace VoiceAiSharp.Ut.Helpers;

internal static class TestModelResolver
{
    private static readonly IReadOnlyList<string> ModelDirectories =
        ResolveModelDirectories();

    public static string GetFirstExistingModelPath(params string[] fileNames)
        => GetFirstExistingModelPathFor("Kokoro", fileNames);

    public static string GetFirstExistingModelPathFor(
        string modelKind,
        params string[] fileNames)
    {
        foreach (string directory in ModelDirectories)
        {
            foreach (string fileName in fileNames)
            {
                string fullPath = Path.Combine(directory, fileName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        Assert.Inconclusive(
            $"{modelKind} model file not found. Run git lfs pull and build the test project. "
            + "Searched: "
            + string.Join(", ", ModelDirectories));

        throw new InvalidOperationException("Assert.Inconclusive did not stop the test.");
    }

    private static IReadOnlyList<string> ResolveModelDirectories()
    {
        HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);

        AddDirectoryIfExists(
            directories,
            Path.Combine(
                AppContext.BaseDirectory,
                "Engines",
                "Providers",
                "KokoroService",
                "Libraries",
                "Native"));

        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                AddDirectoryIfExists(
                    directories,
                    Path.Combine(directory.FullName, "models"));

                AddDirectoryIfExists(
                    directories,
                    Path.Combine(
                        directory.FullName,
                        "src",
                        "VoiceAiSharp",
                        "VoiceAiSharp",
                        "Engines",
                        "Providers",
                        "KokoroService",
                        "Libraries",
                        "Native"));
            }

            directory = directory.Parent;
        }

        return directories.ToArray();
    }

    private static void AddDirectoryIfExists(
        HashSet<string> directories,
        string directory)
    {
        if (Directory.Exists(directory))
        {
            directories.Add(Path.GetFullPath(directory));
        }
    }
}
