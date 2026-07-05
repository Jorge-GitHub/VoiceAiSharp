using VoiceAiSharp.Core.Domain.Settings.Engines;
using VoiceAiSharp.Engines.Providers.KokoroService.Runtime;

namespace VoiceAiSharp.Ut.ProvidersTests.KokoroUts;

[TestClass]
[DoNotParallelize]
public sealed class KokoroModelResolverTests
{
    [TestMethod]
    public void ResolveModelPath_WithProviderLibrariesNativeFolder_UsesBundledModel()
    {
        string originalCurrentDirectory = Environment.CurrentDirectory;
        string root = Path.Combine(
            Path.GetTempPath(),
            "VoiceAiSharp.Kokoro.Tests",
            Guid.NewGuid().ToString("N"));

        string libraryDirectory = Path.Combine(
            root,
            "Engines",
            "Providers",
            "KokoroService",
            "Libraries",
            "Native");

        Directory.CreateDirectory(libraryDirectory);

        string modelPath = Path.Combine(libraryDirectory, "kokoro-v1.0.onnx");
        File.WriteAllBytes(modelPath, new byte[] { 1 });

        try
        {
            Environment.CurrentDirectory = root;

            string resolvedPath = KokoroModelResolver.ResolveModelPath(new TtsRuntimeSettings());

            Assert.AreEqual(Path.GetFullPath(modelPath), resolvedPath);
        }
        finally
        {
            Environment.CurrentDirectory = originalCurrentDirectory;

            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
