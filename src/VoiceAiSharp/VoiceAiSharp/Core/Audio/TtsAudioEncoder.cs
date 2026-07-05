using VoiceAiSharp.Core.Domain.Audio;
using VoiceAiSharp.Core.Domain.Constants.Enums;
using VoiceAiSharp.Core.Domain.Settings.Audio;
using VoiceAiSharp.Core.Domain.Settings.Clients;
using System.Buffers.Binary;

namespace VoiceAiSharp.Core.Audio;

internal sealed class TtsAudioEncoder
{
    private const int DefaultSourceSampleRate = 24000;

    public TtsAudio Create(
        float[] monoSamples,
        string text,
        TtsLocalClientSettings settings,
        TtsLanguage language,
        TtsVoiceGender gender,
        string modelName,
        int sourceSampleRate = DefaultSourceSampleRate)
    {
        ArgumentNullException.ThrowIfNull(monoSamples);
        ArgumentNullException.ThrowIfNull(settings);

        if (sourceSampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceSampleRate),
                "Source sample rate must be greater than zero.");
        }

        if (settings.Audio.SampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(settings.Audio.SampleRate),
                "Sample rate must be greater than zero.");
        }

        if (settings.Audio.Channels is not (1 or 2))
        {
            throw new NotSupportedException(
                "Only mono and stereo audio output are supported in V1.");
        }

        float[] resampled = Resample(monoSamples, sourceSampleRate, settings.Audio.SampleRate);
        byte[] audioBytes = Encode(resampled, settings.Audio);

        return new TtsAudio
        {
            AudioBytes = audioBytes,
            SampleRate = settings.Audio.SampleRate,
            Channels = settings.Audio.Channels,
            Format = settings.Audio.Format,
            Duration = TimeSpan.FromSeconds(
                resampled.Length / (double)settings.Audio.SampleRate),
            Text = text,
            Language = language,
            Gender = gender,
            VoicePreset = settings.Voice.Preset,
            ModelName = modelName
        };
    }

    public TtsAudio CreateEmpty(string text, TtsLocalClientSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return this.Create(
            [],
            text,
            settings,
            settings.Voice.Language,
            settings.Voice.Gender,
            settings.Runtime.ModelName);
    }

    private byte[] Encode(float[] monoSamples, TtsAudioSettings settings)
    {
        return settings.Format switch
        {
            TtsAudioFormat.Wav => this.EncodeWav(monoSamples, settings.SampleRate, settings.Channels),
            TtsAudioFormat.Pcm16 => this.EncodePcm16(monoSamples, settings.Channels),
            TtsAudioFormat.PcmFloat32 => this.EncodePcmFloat32(monoSamples, settings.Channels),
            _ => throw new NotSupportedException(
                $"Audio format '{settings.Format}' is not supported.")
        };
    }

    private float[] Resample(float[] samples, int sourceSampleRate, int targetSampleRate)
    {
        if (sourceSampleRate == targetSampleRate || samples.Length == 0)
        {
            return samples;
        }

        int targetLength = Math.Max(1,
            (int)Math.Round(samples.Length * (targetSampleRate / (double)sourceSampleRate)));

        float[] output = new float[targetLength];

        for (int i = 0; i < output.Length; i++)
        {
            double sourcePosition = i * (sourceSampleRate / (double)targetSampleRate);
            int index = (int)sourcePosition;
            double fraction = sourcePosition - index;

            if (index >= samples.Length - 1)
            {
                output[i] = samples[^1];
                continue;
            }

            output[i] = (float)(samples[index]
                + ((samples[index + 1] - samples[index]) * fraction));
        }

        return output;
    }

    private byte[] EncodePcm16(float[] monoSamples, int channels)
    {
        byte[] bytes = new byte[monoSamples.Length * channels * sizeof(short)];
        int offset = 0;

        foreach (float sample in monoSamples)
        {
            short value = this.ToPcm16(sample);

            for (int channel = 0; channel < channels; channel++)
            {
                BinaryPrimitives.WriteInt16LittleEndian(
                    bytes.AsSpan(offset, sizeof(short)),
                    value);

                offset += sizeof(short);
            }
        }

        return bytes;
    }

    private byte[] EncodePcmFloat32(float[] monoSamples, int channels)
    {
        byte[] bytes = new byte[monoSamples.Length * channels * sizeof(float)];
        int offset = 0;

        foreach (float sample in monoSamples)
        {
            float value = Math.Clamp(sample, -1f, 1f);

            for (int channel = 0; channel < channels; channel++)
            {
                BinaryPrimitives.WriteSingleLittleEndian(
                    bytes.AsSpan(offset, sizeof(float)),
                    value);

                offset += sizeof(float);
            }
        }

        return bytes;
    }

    private byte[] EncodeWav(float[] monoSamples, int sampleRate, int channels)
    {
        const short audioFormatPcm = 1;
        const short bitsPerSample = 16;
        const int headerSize = 44;

        byte[] pcm = this.EncodePcm16(monoSamples, channels);
        byte[] wav = new byte[headerSize + pcm.Length];

        this.WriteAscii(wav, 0, "RIFF");
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(4, 4), 36 + pcm.Length);
        this.WriteAscii(wav, 8, "WAVE");
        this.WriteAscii(wav, 12, "fmt ");
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(16, 4), 16);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(20, 2), audioFormatPcm);
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(22, 2), (short)channels);
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(24, 4), sampleRate);
        BinaryPrimitives.WriteInt32LittleEndian(
            wav.AsSpan(28, 4),
            sampleRate * channels * (bitsPerSample / 8));
        BinaryPrimitives.WriteInt16LittleEndian(
            wav.AsSpan(32, 2),
            (short)(channels * (bitsPerSample / 8)));
        BinaryPrimitives.WriteInt16LittleEndian(wav.AsSpan(34, 2), bitsPerSample);
        this.WriteAscii(wav, 36, "data");
        BinaryPrimitives.WriteInt32LittleEndian(wav.AsSpan(40, 4), pcm.Length);
        Buffer.BlockCopy(pcm, 0, wav, headerSize, pcm.Length);

        return wav;
    }

    private short ToPcm16(float sample)
    {
        float clamped = Math.Clamp(sample, -1f, 1f);
        return (short)Math.Clamp(
            Math.Round(clamped * short.MaxValue),
            short.MinValue,
            short.MaxValue);
    }

    private void WriteAscii(byte[] target, int offset, string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            target[offset + i] = (byte)value[i];
        }
    }
}
