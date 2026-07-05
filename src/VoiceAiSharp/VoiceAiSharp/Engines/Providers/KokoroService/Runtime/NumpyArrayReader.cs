using System.Buffers.Binary;
using System.Text;
using System.Text.RegularExpressions;

namespace VoiceAiSharp.Engines.Providers.KokoroService.Runtime;

internal static partial class NumpyArrayReader
{
    private static readonly byte[] Magic = [0x93, (byte)'N', (byte)'U', (byte)'M', (byte)'P', (byte)'Y'];

    public static FloatArrayData ReadSingleArray(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        Span<byte> magic = stackalloc byte[Magic.Length];
        ReadExactly(stream, magic);

        if (!magic.SequenceEqual(Magic))
        {
            throw new InvalidDataException("The file is not a NumPy .npy array.");
        }

        int major = stream.ReadByte();
        _ = stream.ReadByte();

        if (major < 1)
        {
            throw new InvalidDataException("Unsupported NumPy array version.");
        }

        int headerLength = major == 1
            ? ReadUInt16(stream)
            : ReadInt32(stream);

        byte[] headerBytes = new byte[headerLength];
        ReadExactly(stream, headerBytes);
        string header = Encoding.ASCII.GetString(headerBytes);

        string descriptor = MatchValue(header, DescrRegex(), "descr");
        if (descriptor is not ("<f4" or "|f4"))
        {
            throw new InvalidDataException(
                $"Only little-endian float32 NumPy arrays are supported. Found '{descriptor}'.");
        }

        string fortranOrder = MatchValue(header, FortranRegex(), "fortran_order");
        if (fortranOrder.Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Fortran-order NumPy arrays are not supported.");
        }

        int[] shape = ParseShape(MatchValue(header, ShapeRegex(), "shape"));
        int count = checked((int)((stream.Length - stream.Position) / sizeof(float)));
        byte[] data = new byte[count * sizeof(float)];
        ReadExactly(stream, data);

        float[] values = new float[count];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = BinaryPrimitives.ReadSingleLittleEndian(
                data.AsSpan(i * sizeof(float), sizeof(float)));
        }

        return new FloatArrayData(values, shape);
    }

    public static FloatArrayData ReadRawSingleArray(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (stream.Length % sizeof(float) != 0)
        {
            throw new InvalidDataException(
                "Raw Kokoro voice vector files must contain float32 data.");
        }

        int count = checked((int)(stream.Length / sizeof(float)));
        byte[] data = new byte[count * sizeof(float)];
        ReadExactly(stream, data);

        float[] values = new float[count];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = BinaryPrimitives.ReadSingleLittleEndian(
                data.AsSpan(i * sizeof(float), sizeof(float)));
        }

        int[] shape = values.Length % 256 == 0
            ? [values.Length / 256, 1, 256]
            : values.Length % 128 == 0
                ? [values.Length / 128, 1, 128]
                : [values.Length];

        return new FloatArrayData(values, shape);
    }

    private static int ReadUInt16(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        ReadExactly(stream, buffer);
        return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
    }

    private static int ReadInt32(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        ReadExactly(stream, buffer);
        return BinaryPrimitives.ReadInt32LittleEndian(buffer);
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        int total = 0;

        while (total < buffer.Length)
        {
            int read = stream.Read(buffer[total..]);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            total += read;
        }
    }

    private static string MatchValue(string header, Regex regex, string name)
    {
        Match match = regex.Match(header);
        if (!match.Success)
        {
            throw new InvalidDataException(
                $"NumPy array header is missing '{name}'.");
        }

        return match.Groups["value"].Value;
    }

    private static int[] ParseShape(string value)
    {
        return value
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToArray();
    }

    [GeneratedRegex("'descr'\\s*:\\s*'(?<value>[^']+)'")]
    private static partial Regex DescrRegex();

    [GeneratedRegex("'fortran_order'\\s*:\\s*(?<value>True|False)")]
    private static partial Regex FortranRegex();

    [GeneratedRegex("'shape'\\s*:\\s*\\((?<value>[^\\)]*)\\)")]
    private static partial Regex ShapeRegex();
}

internal sealed record FloatArrayData(float[] Values, int[] Shape);
