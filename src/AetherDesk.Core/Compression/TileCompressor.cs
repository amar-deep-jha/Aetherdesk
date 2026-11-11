using System.IO.Compression;
using AetherDesk.Core.Protocol.Messages;

namespace AetherDesk.Core.Compression;

/// <summary>
/// Compresses and decompresses screen tile pixel buffers.
/// </summary>
public static class TileCompressor
{
    /// <summary>
    /// Compresses a raw BGRA tile pixel buffer using the specified compression format.
    /// </summary>
    public static byte[] CompressTile(ReadOnlySpan<byte> rawBgra, TileCompressionFormat format = TileCompressionFormat.Deflate)
    {
        if (format == TileCompressionFormat.RawBgra)
        {
            return rawBgra.ToArray();
        }

        // Deflate compression
        using var outputStream = new MemoryStream();
        using (var deflate = new DeflateStream(outputStream, CompressionLevel.Fastest, leaveOpen: true))
        {
            deflate.Write(rawBgra);
        }
        return outputStream.ToArray();
    }

    /// <summary>
    /// Decompresses a compressed tile buffer back to raw BGRA pixels.
    /// </summary>
    public static byte[] DecompressTile(ReadOnlySpan<byte> compressedData, int uncompressedSize, TileCompressionFormat format = TileCompressionFormat.Deflate)
    {
        if (format == TileCompressionFormat.RawBgra)
        {
            return compressedData.ToArray();
        }

        byte[] destination = new byte[uncompressedSize];
        using var inputStream = new MemoryStream(compressedData.ToArray());
        using var deflate = new DeflateStream(inputStream, CompressionMode.Decompress);

        int totalRead = 0;
        while (totalRead < uncompressedSize)
        {
            int read = deflate.Read(destination, totalRead, uncompressedSize - totalRead);
            if (read == 0)
                break;
            totalRead += read;
        }

        return destination;
    }
}
