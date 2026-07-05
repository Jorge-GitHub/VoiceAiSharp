namespace VoiceAiSharp.Ut.Helpers;

internal static class TestModelResolver
{
    private static readonly string ModelsDirectory = ResolveModelsDirectory();

    public static string GetFirstExistingModelPath(params string[] fileNames)
        => GetFirstExistingModelPathFor("Kokoro", fileNames);

    public static string GetFirstExistingModelPathFor(
        string modelKind,
        params string[] fileNames)
    {
        foreach (string fileName in fileNames)
        {
            string fullPath = Path.Combine(ModelsDirectory, fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        Assert.Inconclusive(
            $"{modelKind} model file not found. Download a {modelKind} ONNX model into "
            + $"'{ModelsDirectory}' to run this integration test.");

        throw new InvalidOperationException("Assert.Inconclusive did not stop the test.");
    }

    private static string ResolveModelsDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return Path.Combine(directory.FullName, "models");
            }

            directory = directory.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "models");
    }
}
