using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Core.Engines.Base;
using VoiceAiSharp.Core.Engines.Repositories;

namespace VoiceAiSharp.Ut.Core;

[TestClass]
public sealed class EngineRepositoryTests
{
    [TestMethod]
    public void GetEngine_ReusesSameRuntimeConfiguration()
    {
        using EngineRepository repository = new();
        TtsLocalClientSettings settings = new();

        TtsEngineBase first = repository.GetEngine(settings);
        TtsEngineBase second = repository.GetEngine(settings);

        Assert.AreSame(first, second);
        Assert.AreEqual(1, repository.Count);
        Assert.AreEqual(TtsEngineType.Kokoro, first.Type);
        Assert.AreEqual(TtsBackend.Cpu, first.Backend);
    }

    [TestMethod]
    public void GetEngine_CreatesNewEngineForDifferentModel()
    {
        using EngineRepository repository = new();
        TtsLocalClientSettings firstSettings = new();
        TtsLocalClientSettings secondSettings = new();

        firstSettings.Runtime.ModelName = "kokoro-a";
        secondSettings.Runtime.ModelName = "kokoro-b";

        TtsEngineBase first = repository.GetEngine(firstSettings);
        TtsEngineBase second = repository.GetEngine(secondSettings);

        Assert.AreNotSame(first, second);
        Assert.AreEqual(2, repository.Count);
    }

    [TestMethod]
    public void GetEngine_CreatesCudaKokoroEngine()
    {
        using EngineRepository repository = new();
        TtsLocalClientSettings settings = new();

        settings.Runtime.Backend = TtsBackend.Cuda;

        TtsEngineBase engine = repository.GetEngine(settings);

        Assert.AreEqual(TtsEngineType.Kokoro, engine.Type);
        Assert.AreEqual(TtsBackend.Cuda, engine.Backend);
    }

    [TestMethod]
    public void GetEngine_CreatesDirectMlKokoroEngine()
    {
        using EngineRepository repository = new();
        TtsLocalClientSettings settings = new();

        settings.Runtime.Backend = TtsBackend.DirectML;

        TtsEngineBase engine = repository.GetEngine(settings);

        Assert.AreEqual(TtsEngineType.Kokoro, engine.Type);
        Assert.AreEqual(TtsBackend.DirectML, engine.Backend);
    }

    [TestMethod]
    public void GetEngine_CreatesPiperEngine()
    {
        using EngineRepository repository = new();
        TtsLocalClientSettings settings = new();

        settings.Runtime.Engine = TtsEngineType.Piper;

        TtsEngineBase engine = repository.GetEngine(settings);

        Assert.AreEqual(TtsEngineType.Piper, engine.Type);
        Assert.AreEqual(TtsBackend.Cpu, engine.Backend);
    }

    [TestMethod]
    public void GetEngine_RejectsUnimplementedEngine()
    {
        using EngineRepository repository = new();
        TtsLocalClientSettings settings = new();

        settings.Runtime.Engine = TtsEngineType.FishSpeech;

        try
        {
            repository.GetEngine(settings);
            Assert.Fail("Unimplemented engines should throw.");
        }
        catch (NotImplementedException)
        {
        }
    }
}
