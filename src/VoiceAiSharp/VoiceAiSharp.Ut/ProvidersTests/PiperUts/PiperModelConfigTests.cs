using VoiceAiSharp.Engines.Providers.PiperService.Runtime;

namespace VoiceAiSharp.Ut.ProvidersTests.PiperUts;

[TestClass]
public sealed class PiperModelConfigTests
{
    [TestMethod]
    public void Load_WithPiperConfig_ReadsRuntimeSettings()
    {
        string configPath = this.WriteConfig(CreateConfigJson());

        PiperModelConfig config = PiperModelConfig.Load(configPath);

        Assert.AreEqual(6, config.NumSymbols);
        Assert.AreEqual(1, config.NumSpeakers);
        Assert.AreEqual(16000, config.SampleRate);
        Assert.AreEqual(0.667f, config.NoiseScale);
        Assert.AreEqual(1.1f, config.LengthScale);
        Assert.AreEqual(0.8f, config.NoiseW);
        Assert.AreEqual("en-us", config.EspeakVoice);
        Assert.AreEqual("en_US", config.LanguageCode);
        Assert.AreEqual(PiperPhonemeType.Espeak, config.PhonemeType);
        CollectionAssert.AreEqual(new[] { 1 }, config.PhonemeIdMap["^"]);
    }

    [TestMethod]
    public void Load_WithMissingSpecialToken_ThrowsClearException()
    {
        string configPath = this.WriteConfig(
            """
            {
              "audio": { "sample_rate": 16000 },
              "num_symbols": 2,
              "num_speakers": 1,
              "phoneme_id_map": {
                "^": [1],
                "$": [2]
              }
            }
            """);

        try
        {
            PiperModelConfig.Load(configPath);
            Assert.Fail("Invalid Piper configs should throw.");
        }
        catch (InvalidDataException exception)
        {
            StringAssert.Contains(exception.Message, "phoneme_id_map entry '_'");
        }
    }

    internal string WriteConfig(string json)
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "VoiceAiSharp.Piper.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        string configPath = Path.Combine(directory, "model.onnx.json");
        File.WriteAllText(configPath, json);

        return configPath;
    }

    internal static string CreateConfigJson()
    {
        return
            """
            {
              "audio": {
                "sample_rate": 16000,
                "quality": "low"
              },
              "espeak": {
                "voice": "en-us"
              },
              "inference": {
                "noise_scale": 0.667,
                "length_scale": 1.1,
                "noise_w": 0.8
              },
              "phoneme_id_map": {
                "_": [0],
                "^": [1],
                "$": [2],
                " ": [3],
                "!": [4],
                "a": [14],
                "b": [15]
              },
              "num_symbols": 6,
              "num_speakers": 1,
              "language": {
                "code": "en_US",
                "name_english": "English"
              },
              "dataset": "amy"
            }
            """;
    }
}
