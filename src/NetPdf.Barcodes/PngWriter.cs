using System.Buffers.Binary;
using System.IO.Compression;

namespace NetPdf.Creation;

/// <summary>Minimal 8-bit grayscale PNG encoder, used to embed generated barcode bitmaps.</summary>
internal static class PngWriter
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Encodes an 8-bit grayscale pixel grid ([row, column], 0 = black) as a PNG.</summary>
    internal static byte[] WriteGrayscale(byte[,] pixels)
    {
        var height = pixels.GetLength(0);
        var width = pixels.GetLength(1);

        using var output = new MemoryStream();
        output.Write(Signature);

        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr, width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr[4..], height);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 0;  // color type: grayscale
        WriteChunk(output, "IHDR", ihdr.ToArray());

        // Scanlines, each prefixed with filter type 0 (None), zlib-compressed.
        var raw = new byte[height * (width + 1)];
        var offset = 0;
        for (var y = 0; y < height; y++)
        {
            raw[offset++] = 0;
            for (var x = 0; x < width; x++)
                raw[offset++] = pixels[y, x];
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(raw);
        WriteChunk(output, "IDAT", compressed.ToArray());

        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        Span<byte> header = stackalloc byte[8];
        BinaryPrimitives.WriteInt32BigEndian(header, data.Length);
        header[4] = (byte)type[0];
        header[5] = (byte)type[1];
        header[6] = (byte)type[2];
        header[7] = (byte)type[3];
        output.Write(header);
        output.Write(data);

        var crc = Crc32(header[4..], data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        output.Write(crcBytes);
    }

    private static uint Crc32(ReadOnlySpan<byte> typeBytes, ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in typeBytes)
            crc = Step(crc, b);
        foreach (var b in data)
            crc = Step(crc, b);
        return crc ^ 0xFFFFFFFFu;

        static uint Step(uint crc, byte b)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc >> 1) ^ (0xEDB88320u & (uint)-(crc & 1));
            return crc;
        }
    }
}
