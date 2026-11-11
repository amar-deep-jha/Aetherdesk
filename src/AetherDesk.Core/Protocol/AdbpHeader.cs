using System.Buffers.Binary;

namespace AetherDesk.Core.Protocol;

/// <summary>
/// Represents the fixed 12-byte binary header for all ADBP packets.
/// Layout:
///   [0..3]  Magic: 'A', 'D', 'B', 'P' (0x41, 0x44, 0x42, 0x50)
///   [4]     Version: 0x01
///   [5]     Flags: AdbpFlags
///   [6]     OpCode: AdbpOpCode
///   [7]     Reserved: 0x00
///   [8..11] PayloadLength: 32-bit unsigned integer (big-endian)
/// </summary>
public readonly struct AdbpHeader : IEquatable<AdbpHeader>
{
    public const int HeaderSize = 12;
    public static readonly byte[] MagicBytes = "ADBP"u8.ToArray();
    public const byte CurrentVersion = 0x01;

    public byte Version { get; }
    public AdbpFlags Flags { get; }
    public AdbpOpCode OpCode { get; }
    public uint PayloadLength { get; }

    public AdbpHeader(AdbpOpCode opCode, uint payloadLength, AdbpFlags flags = AdbpFlags.None, byte version = CurrentVersion)
    {
        Version = version;
        Flags = flags;
        OpCode = opCode;
        PayloadLength = payloadLength;
    }

    public void WriteTo(Span<byte> destination)
    {
        if (destination.Length < HeaderSize)
            throw new ArgumentException($"Destination span too small. Required: {HeaderSize}, Actual: {destination.Length}");

        destination[0] = (byte)'A';
        destination[1] = (byte)'D';
        destination[2] = (byte)'B';
        destination[3] = (byte)'P';
        destination[4] = Version;
        destination[5] = (byte)Flags;
        destination[6] = (byte)OpCode;
        destination[7] = 0x00; // Reserved
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(8, 4), PayloadLength);
    }

    public static bool TryParse(ReadOnlySpan<byte> source, out AdbpHeader header)
    {
        header = default;
        if (source.Length < HeaderSize)
            return false;

        if (source[0] != 'A' || source[1] != 'D' || source[2] != 'B' || source[3] != 'P')
            return false;

        byte version = source[4];
        var flags = (AdbpFlags)source[5];
        var opCode = (AdbpOpCode)source[6];
        uint payloadLength = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(8, 4));

        header = new AdbpHeader(opCode, payloadLength, flags, version);
        return true;
    }

    public byte[] ToBytes()
    {
        byte[] bytes = new byte[HeaderSize];
        WriteTo(bytes);
        return bytes;
    }

    public bool Equals(AdbpHeader other) =>
        Version == other.Version &&
        Flags == other.Flags &&
        OpCode == other.OpCode &&
        PayloadLength == other.PayloadLength;

    public override bool Equals(object? obj) => obj is AdbpHeader other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Version, (byte)Flags, (byte)OpCode, PayloadLength);
    public static bool operator ==(AdbpHeader left, AdbpHeader right) => left.Equals(right);
    public static bool operator !=(AdbpHeader left, AdbpHeader right) => !left.Equals(right);
}
