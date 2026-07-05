using VoiceAiSharp.Engines.Providers.PiperService.Runtime;
using VoiceAiSharp.Engines.Providers.PiperService.Text;

namespace VoiceAiSharp.Ut.ProvidersTests.PiperUts;

[TestClass]
public sealed class PiperPhonemeTokenizerTests
{
    [TestMethod]
    public void Tokenize_MapsPhonemesWithPiperBoundaryAndPadTokens()
    {
        PiperModelConfig config = this.CreateConfig();
        PiperPhonemeTokenizer tokenizer = new(config);

        int[] ids = tokenizer.Tokenize("ab!");

        CollectionAssert.AreEqual(
            new[] { 1, 14, 0, 15, 0, 4, 0, 2 },
            ids);
    }

    [TestMethod]
    public void Tokenize_WithUnknownCharacters_SkipsUnknownCharacters()
    {
        PiperModelConfig config = this.CreateConfig();
        PiperPhonemeTokenizer tokenizer = new(config);

        int[] ids = tokenizer.Tokenize("a#b");

        CollectionAssert.AreEqual(
            new[] { 1, 14, 0, 15, 0, 2 },
            ids);
    }

    private PiperModelConfig CreateConfig()
    {
        PiperModelConfigTests helper = new();
        string configPath = helper.WriteConfig(PiperModelConfigTests.CreateConfigJson());

        return PiperModelConfig.Load(configPath);
    }
}
