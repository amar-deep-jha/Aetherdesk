using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AetherDesk.Core.Protocol.Messages;

public enum TileCompressionFormat : byte
{
    RawBgra = 0,
    Deflate = 1,
    Lz4 = 2,
    Jpeg = 3
}

public sealed class ScreenResolutionMsg
{
    [JsonPropertyName("mon")]
    public int MonitorIndex { get; set; }

    [JsonPropertyName("w")]
    public int Width { get; set; }

    [JsonPropertyName("h")]
    public int Height { get; set; }

    [JsonPropertyName("dpi")]
    public int Dpi { get; set; } = 96;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static ScreenResolutionMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<ScreenResolutionMsg>(bytes);
}

public sealed class MonitorInfoMsg
{
    [JsonPropertyName("idx")]
    public int Index { get; set; }

    [JsonPropertyName("name")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("w")]
    public int Width { get; set; }

    [JsonPropertyName("h")]
    public int Height { get; set; }

    [JsonPropertyName("pri")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("x")]
    public int Left { get; set; }

    [JsonPropertyName("y")]
    public int Top { get; set; }
}

public sealed class MonitorListMsg
{
    [JsonPropertyName("monitors")]
    public List<MonitorInfoMsg> Monitors { get; set; } = new();

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static MonitorListMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<MonitorListMsg>(bytes);
}

public sealed class SelectMonitorMsg
{
    [JsonPropertyName("idx")]
    public int MonitorIndex { get; set; }

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static SelectMonitorMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<SelectMonitorMsg>(bytes);
}

/// <summary>
/// Compact binary payload for screen tiles:
/// Layout (20-byte tile header + compressed pixel payload):
///   [0..3]   FrameId (uint)
///   [4]      MonitorIndex (byte)
///   [5]      CompressionFormat (byte)
///   [6..7]   TileX (ushort)
///   [8..9]   TileY (ushort)
///   [10..11] TileWidth (ushort)
///   [12..13] TileHeight (ushort)
///   [14..17] UncompressedSize (uint)
///   [18..19] Reserved (ushort)
///   [20..]   CompressedData
/// </summary>
public sealed class ScreenTileFrameMsg
{
    public const int HeaderSize = 20;

    public uint FrameId { get; set; }
    public byte MonitorIndex { get; set; }
    public TileCompressionFormat Format { get; set; }
    public ushort TileX { get; set; }
    public ushort TileY { get; set; }
    public ushort TileWidth { get; set; }
    public ushort TileHeight { get; set; }
    public uint UncompressedSize { get; set; }
    public ReadOnlyMemory<byte> Data { get; set; }

    public byte[] Serialize()
    {
        byte[] buffer = new byte[HeaderSize + Data.Length];
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(0, 4), FrameId);
        buffer[4] = MonitorIndex;
        buffer[5] = (byte)Format;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(6, 2), TileX);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(8, 2), TileY);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(10, 2), TileWidth);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(12, 2), TileHeight);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(14, 4), UncompressedSize);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(18, 2), 0);
        Data.Span.CopyTo(buffer.AsSpan(HeaderSize));
        return buffer;
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> bytes, out ScreenTileFrameMsg? msg)
    {
        msg = null;
        if (bytes.Length < HeaderSize)
            return false;

        uint frameId = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(0, 4));
        byte monIndex = bytes[4];
        var format = (TileCompressionFormat)bytes[5];
        ushort x = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(6, 2));
        ushort y = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(8, 2));
        ushort w = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(10, 2));
        ushort h = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(12, 2));
        uint uncompressedSize = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(14, 4));

        byte[] data = bytes.Slice(HeaderSize).ToArray();
        msg = new ScreenTileFrameMsg
        {
            FrameId = frameId,
            MonitorIndex = monIndex,
            Format = format,
            TileX = x,
            TileY = y,
            TileWidth = w,
            TileHeight = h,
            UncompressedSize = uncompressedSize,
            Data = data
        };
        return true;
    }
}

public sealed class CursorPositionMsg
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("v")]
    public bool Visible { get; set; } = true;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static CursorPositionMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<CursorPositionMsg>(bytes);
}

public sealed class CursorShapeMsg
{
    [JsonPropertyName("type")]
    public string CursorType { get; set; } = "Arrow";

    [JsonPropertyName("w")]
    public int Width { get; set; }

    [JsonPropertyName("h")]
    public int Height { get; set; }

    [JsonPropertyName("hx")]
    public int HotspotX { get; set; }

    [JsonPropertyName("hy")]
    public int HotspotY { get; set; }

    [JsonPropertyName("rgba")]
    public string RgbaBase64 { get; set; } = string.Empty;

    public byte[] Serialize() => JsonSerializer.SerializeToUtf8Bytes(this);
    public static CursorShapeMsg? Deserialize(ReadOnlySpan<byte> bytes) => JsonSerializer.Deserialize<CursorShapeMsg>(bytes);
}
