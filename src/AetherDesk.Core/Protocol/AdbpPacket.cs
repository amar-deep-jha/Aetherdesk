namespace AetherDesk.Core.Protocol;

/// <summary>
/// Represents a complete ADBP packet containing an 8-byte header and a payload buffer.
/// </summary>
public sealed class AdbpPacket
{
    public AdbpHeader Header { get; }
    public ReadOnlyMemory<byte> Payload { get; }

    public AdbpOpCode OpCode => Header.OpCode;
    public AdbpFlags Flags => Header.Flags;
    public uint PayloadLength => Header.PayloadLength;

    public AdbpPacket(AdbpHeader header, ReadOnlyMemory<byte> payload)
    {
        if (header.PayloadLength != payload.Length)
            throw new ArgumentException($"Payload length ({payload.Length}) must match header length ({header.PayloadLength}).");

        Header = header;
        Payload = payload;
    }

    public AdbpPacket(AdbpOpCode opCode, ReadOnlyMemory<byte> payload, AdbpFlags flags = AdbpFlags.None)
        : this(new AdbpHeader(opCode, (uint)payload.Length, flags), payload)
    {
    }

    public static AdbpPacket CreateEmpty(AdbpOpCode opCode, AdbpFlags flags = AdbpFlags.None)
    {
        return new AdbpPacket(new AdbpHeader(opCode, 0, flags), ReadOnlyMemory<byte>.Empty);
    }

    public byte[] Serialize()
    {
        byte[] buffer = new byte[AdbpHeader.HeaderSize + Payload.Length];
        Header.WriteTo(buffer.AsSpan(0, AdbpHeader.HeaderSize));
        Payload.Span.CopyTo(buffer.AsSpan(AdbpHeader.HeaderSize));
        return buffer;
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out AdbpPacket? packet, out int consumed)
    {
        packet = null;
        consumed = 0;

        if (data.Length < AdbpHeader.HeaderSize)
            return false;

        if (!AdbpHeader.TryParse(data.Slice(0, AdbpHeader.HeaderSize), out var header))
            return false;

        int totalLength = AdbpHeader.HeaderSize + (int)header.PayloadLength;
        if (data.Length < totalLength)
            return false;

        byte[] payload = data.Slice(AdbpHeader.HeaderSize, (int)header.PayloadLength).ToArray();
        packet = new AdbpPacket(header, payload);
        consumed = totalLength;
        return true;
    }
}
