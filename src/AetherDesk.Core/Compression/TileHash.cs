using System.IO.Hashing;

namespace AetherDesk.Core.Compression;

/// <summary>
/// Provides sub-microsecond 64-bit hashing for screen tile difference detection using XxHash64.
/// </summary>
public static class TileHash
{
    public static ulong Compute(ReadOnlySpan<byte> pixelBytes)
    {
        return XxHash64.HashToUInt64(pixelBytes);
    }
}
