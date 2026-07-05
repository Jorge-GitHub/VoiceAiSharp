namespace VoiceAiSharp.Core.Domain.Constants.Enums;

public enum TtsEngineType
{
    Auto = 0,
    Kokoro = 1,
    Piper = 2,
    FishSpeech = 3
}

public enum TtsBackend
{
    Auto = 0,
    Cpu = 1,
    DirectML = 2,
    Cuda = 3
}

public enum TtsAudioFormat
{
    Wav = 0,
    Pcm16 = 1,
    PcmFloat32 = 2
}

public enum TtsLanguage
{
    Auto = 0,
    AmericanEnglish = 1,
    BritishEnglish = 2,
    Japanese = 3,
    MandarinChinese = 4,
    Spanish = 5,
    French = 6,
    Hindi = 7,
    Italian = 8,
    BrazilianPortuguese = 9
}

public enum TtsVoiceGender
{
    Any = 0,
    Female = 1,
    Male = 2,
    Neutral = 3
}

public enum TtsVoicePreset
{
    Default = 0,
    Heart = 1,
    George = 2,
    Michael = 3,
    Emma = 4,
    Bella = 5
}
