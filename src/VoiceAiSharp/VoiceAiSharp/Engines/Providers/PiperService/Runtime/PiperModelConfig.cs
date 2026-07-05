using System.Text.Json;

namespace VoiceAiSharp.Engines.Providers.PiperService.Runtime;

internal sealed class PiperModelConfig
{
    public int NumSymbols { get; private init; }
    public int NumSpeakers { get; private init; }
    public int SampleRate { get; private init; }
    public float NoiseScale { get; private init; }
    public float LengthScale { get; private init; }
    public float NoiseW { get; private init; }
    public string EspeakVoice { get; private init; } = "";
    public string LanguageCode { get; private init; } = "";
    public string LanguageName { get; private init; } = "";
    public string Dataset { get; private init; } = "";
    public PiperPhonemeType PhonemeType { get; private init; }
    public IReadOnlyDictionary<string, int[]> PhonemeIdMap { get; private init; }
        = new Dictionary<string, int[]>(StringComparer.Ordinal);

    public static PiperModelConfig Load(string configPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);

        string fullPath = Path.GetFullPath(configPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Piper model config file was not found: {fullPath}",
                fullPath);
        }

        using FileStream stream = File.OpenRead(fullPath);
        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement root = document.RootElement;
        JsonElement audio = GetRequiredObject(root, "audio");
        JsonElement inference = GetOptionalObject(root, "inference");
        JsonElement language = GetOptionalObject(root, "language");
        JsonElement espeak = GetOptionalObject(root, "espeak");

        int sampleRate = GetRequiredInt(audio, "sample_rate");
        string languageCode = GetOptionalString(language, "code");
        string espeakVoice = GetOptionalString(espeak, "voice");

        if (string.IsNullOrWhiteSpace(espeakVoice))
        {
            espeakVoice = CreateEspeakVoiceFromLanguageCode(languageCode);
        }

        PiperModelConfig config = new()
        {
            NumSymbols = GetRequiredInt(root, "num_symbols"),
            NumSpeakers = GetOptionalInt(root, "num_speakers", 1),
            SampleRate = sampleRate,
            NoiseScale = GetOptionalFloat(inference, "noise_scale", 0.667f),
            LengthScale = GetOptionalFloat(inference, "length_scale", 1.0f),
            NoiseW = GetOptionalFloat(inference, "noise_w", 0.8f),
            EspeakVoice = espeakVoice,
            LanguageCode = languageCode,
            LanguageName = GetOptionalString(language, "name_english"),
            Dataset = GetOptionalString(root, "dataset"),
            PhonemeType = ParsePhonemeType(GetOptionalString(root, "phoneme_type")),
            PhonemeIdMap = ReadPhonemeIdMap(GetRequiredObject(root, "phoneme_id_map"))
        };

        config.Validate(fullPath);

        return config;
    }

    private void Validate(string configPath)
    {
        if (this.SampleRate <= 0)
        {
            throw new InvalidDataException(
                $"Piper config '{configPath}' has an invalid audio.sample_rate.");
        }

        if (this.NumSymbols <= 0)
        {
            throw new InvalidDataException(
                $"Piper config '{configPath}' has an invalid num_symbols.");
        }

        if (this.NumSpeakers <= 0)
        {
            throw new InvalidDataException(
                $"Piper config '{configPath}' has an invalid num_speakers.");
        }

        if (this.PhonemeIdMap.Count == 0)
        {
            throw new InvalidDataException(
                $"Piper config '{configPath}' has no phoneme_id_map entries.");
        }

        foreach (string specialSymbol in new[] { "^", "_", "$" })
        {
            if (!this.PhonemeIdMap.ContainsKey(specialSymbol))
            {
                throw new InvalidDataException(
                    $"Piper config '{configPath}' is missing phoneme_id_map entry '{specialSymbol}'.");
            }
        }

        if (!float.IsFinite(this.NoiseScale)
            || !float.IsFinite(this.LengthScale)
            || !float.IsFinite(this.NoiseW)
            || this.LengthScale <= 0f)
        {
            throw new InvalidDataException(
                $"Piper config '{configPath}' has invalid inference scale values.");
        }
    }

    private static JsonElement GetRequiredObject(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException(
                $"Piper config is missing required object '{propertyName}'.");
        }

        return property;
    }

    private static JsonElement GetOptionalObject(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.Object)
        {
            return property;
        }

        return default;
    }

    private static int GetRequiredInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.Number
            || !property.TryGetInt32(out int value))
        {
            throw new InvalidDataException(
                $"Piper config is missing required integer '{propertyName}'.");
        }

        return value;
    }

    private static int GetOptionalInt(
        JsonElement element,
        string propertyName,
        int defaultValue)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out int value))
        {
            return value;
        }

        return defaultValue;
    }

    private static float GetOptionalFloat(
        JsonElement element,
        string propertyName,
        float defaultValue)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDouble(out double value))
        {
            return (float)value;
        }

        return defaultValue;
    }

    private static string GetOptionalString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out JsonElement property)
            && property.ValueKind == JsonValueKind.String)
        {
            return property.GetString() ?? "";
        }

        return "";
    }

    private static IReadOnlyDictionary<string, int[]> ReadPhonemeIdMap(JsonElement element)
    {
        Dictionary<string, int[]> map = new(StringComparer.Ordinal);

        foreach (JsonProperty property in element.EnumerateObject())
        {
            int[] ids = ReadIdArray(property.Value);
            if (ids.Length > 0)
            {
                map[property.Name] = ids;
            }
        }

        return map;
    }

    private static int[] ReadIdArray(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number
            && element.TryGetInt32(out int singleValue))
        {
            return [singleValue];
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<int> values = new();

        foreach (JsonElement item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Number
                && item.TryGetInt32(out int value))
            {
                values.Add(value);
            }
        }

        return values.ToArray();
    }

    private static PiperPhonemeType ParsePhonemeType(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "" or "espeak" => PiperPhonemeType.Espeak,
            "text" => PiperPhonemeType.Text,
            _ => throw new InvalidDataException(
                $"Piper phoneme_type '{value}' is not supported.")
        };
    }

    private static string CreateEspeakVoiceFromLanguageCode(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return "en-us";
        }

        return languageCode.Replace('_', '-').ToLowerInvariant();
    }
}
