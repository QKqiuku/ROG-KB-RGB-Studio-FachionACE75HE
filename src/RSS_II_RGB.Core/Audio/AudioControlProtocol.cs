using System.Buffers.Binary;
using System.Text.Json;

namespace RSS_II_RGB.Core.Audio;

public static class AudioControlProtocol
{
    public const int MaxBytes = 1024 * 1024;
    public static async Task<byte[]> ReadAsync(Stream stream, CancellationToken ct)
    {
        byte[] header = new byte[4]; await stream.ReadExactlyAsync(header, ct);
        int size = BinaryPrimitives.ReadInt32LittleEndian(header);
        if (size < 1 || size > MaxBytes) throw new InvalidDataException("Audio control message exceeds its size limit.");
        byte[] data = new byte[size]; await stream.ReadExactlyAsync(data, ct); return data;
    }
    public static async Task WriteAsync(Stream stream, byte[] data, CancellationToken ct)
    {
        if (data.Length > MaxBytes) throw new InvalidDataException("Audio control message exceeds its size limit.");
        byte[] header = new byte[4]; BinaryPrimitives.WriteInt32LittleEndian(header, data.Length);
        await stream.WriteAsync(header, ct); await stream.WriteAsync(data, ct); await stream.FlushAsync(ct);
    }
}
