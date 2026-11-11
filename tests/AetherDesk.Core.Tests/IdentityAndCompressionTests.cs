using AetherDesk.Core.Compression;
using AetherDesk.Core.Identity;
using AetherDesk.Core.Protocol.Messages;

namespace AetherDesk.Core.Tests;

public class IdentityAndCompressionTests
{
    [Fact]
    public void DeviceIdGenerator_GeneratesValid9DigitIdWithLuhnChecksum()
    {
        string id = DeviceIdGenerator.GeneratePersistentDeviceId("TEST-HARDWARE-FP-001");

        Assert.Equal(9, id.Length);
        Assert.True(DeviceIdGenerator.IsValid(id));

        string formatted = DeviceIdGenerator.Format(id);
        Assert.Matches(@"^\d{3} \d{3} \d{3}$", formatted);
    }

    [Fact]
    public void DeviceIdGenerator_RejectsInvalidOrCorruptedIds()
    {
        string validId = DeviceIdGenerator.GeneratePersistentDeviceId("TEST-HARDWARE-FP-002");
        Assert.True(DeviceIdGenerator.IsValid(validId));

        // Corrupt the check digit
        char lastChar = validId[^1];
        char corruptedLast = lastChar == '9' ? '0' : (char)(lastChar + 1);
        string corruptedId = validId.Substring(0, 8) + corruptedLast;

        Assert.False(DeviceIdGenerator.IsValid(corruptedId));
        Assert.False(DeviceIdGenerator.IsValid("12345678")); // too short
        Assert.False(DeviceIdGenerator.IsValid("1234567890")); // too long
    }

    [Fact]
    public void TileHash_DetectsChangedPixels()
    {
        byte[] tileA = new byte[64 * 64 * 4];
        byte[] tileB = new byte[64 * 64 * 4];

        ulong hashA1 = TileHash.Compute(tileA);
        ulong hashA2 = TileHash.Compute(tileA);
        Assert.Equal(hashA1, hashA2);

        // Modify a single pixel
        tileB[100] = 0xAA;
        ulong hashB = TileHash.Compute(tileB);
        Assert.NotEqual(hashA1, hashB);
    }

    [Fact]
    public void TileCompressor_CompressesAndDecompressesAccurately()
    {
        byte[] original = new byte[64 * 64 * 4];
        for (int i = 0; i < original.Length; i++)
        {
            original[i] = (byte)(i % 256);
        }

        byte[] compressed = TileCompressor.CompressTile(original, TileCompressionFormat.Deflate);
        Assert.True(compressed.Length < original.Length);

        byte[] decompressed = TileCompressor.DecompressTile(compressed, original.Length, TileCompressionFormat.Deflate);
        Assert.Equal(original, decompressed);
    }
}
