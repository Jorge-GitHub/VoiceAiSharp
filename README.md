# VoiceAiSharp

AI-powered text-to-speech for .NET with a simple, provider-agnostic API.

VoiceAiSharp is an open-source C#/.NET library for local AI-powered text-to-speech that provides 
a unified API for local speech synthesis engines. Switch providers without changing your application code.

## Status

VoiceAiSharp currently supports Kokoro. Piper and other TTS models are still in progress.

The library can return generated audio in memory, save it to a file, or write it to any .NET `Stream` such as an HTTP response stream, file stream, or memory stream.

## Requirements

- .NET 10
- The bundled Kokoro assets:
  - `kokoro-v1.0.onnx`
  - `voices-v1.0.bin`

The Kokoro model and voice data are included under `Engines/Providers/KokoroService/Libraries/Native` and are copied to the build output by default. You do not need to create a `models` folder for the default Kokoro setup.

If the model file is missing after cloning the repository, make sure Git LFS downloaded it:

```powershell
git lfs pull
```

## Quick Start

```csharp
using VoiceAiSharp.Core;
using VoiceAiSharp.Core.Domain.Audio;
using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using VoiceAiSharp.Extensions.Core.Domain.Audio;

TtsLocalClientSettings settings = new()
{
    Runtime =
    {
        Engine = TtsEngineType.Kokoro,
        Backend = TtsBackend.Cpu
    },
    Voice =
    {
        Language = TtsLanguage.BritishEnglish,
        Gender = TtsVoiceGender.Male,
        Preset = TtsVoicePreset.George,
        Speed = 1.0f
    },
    Audio =
    {
        Format = TtsAudioFormat.Wav
    }
};

using TtsLocalClient tts = new(settings);

TtsAudio audio = await tts.SpeakAsync("Hello from VoiceAiSharp.");
await audio.SaveAsync("hello.wav");
```

By default, VoiceAiSharp resolves the Kokoro model from the copied build output:

```text
bin/<configuration>/<target-framework>/Engines/Providers/KokoroService/Libraries/Native
```

Set `TtsRuntimeSettings.ModelPath` only when you want to use a custom Kokoro model outside the bundled output.

## Write Audio To A Stream

Use `WriteToAsync` when you want to stream the generated audio bytes somewhere other than a file path.

```csharp
using MemoryStream stream = new();

TtsAudio audio = await tts.SpeakAsync("This audio is written to a stream.");
await audio.WriteToAsync(stream);

byte[] wavBytes = stream.ToArray();
```

For ASP.NET endpoints, the same extension can write directly to the response body:

```csharp
TtsAudio audio = await tts.SpeakAsync("Streaming from an HTTP endpoint.");

response.ContentType = "audio/wav";
await audio.WriteToAsync(response.Body, cancellationToken);
```

## Save Audio Safely

If you prefer a no-throw helper for optional audio or optional paths, use `TrySaveAsync` or `TryWriteToAsync`.

```csharp
TtsAudio audio = await tts.SpeakAsync("Save this if possible.");

bool saved = await audio.TrySaveAsync("speech.wav");
```

## Supported Engines

| Engine | Status |
| --- | --- |
| Kokoro | Supported |
| Piper | In progress |
| Other TTS models | Planned |
