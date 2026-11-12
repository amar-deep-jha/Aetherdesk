using System.Buffers.Binary;
using System.IO;

namespace AetherDesk.Core.Protocol.Messages;

/// <summary>
/// Packages multiple screen tiles into a single coalesced network frame to eliminate per-tile packet storm.
/// </summary>
public sealed class ScreenTileBatchMsg
{
    public uint FrameId { get; set; }
    public List<ScreenTileFrameMsg> Tiles { get; set; } = new();

    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(FrameId);
        writer.Write((ushort)Tiles.Count);

        foreach (var tile in Tiles)
        {
            writer.Write(tile.TileX);
            writer.Write(tile.TileY);
            writer.Write(tile.TileWidth);
            writer.Write(tile.TileHeight);
            writer.Write((byte)tile.Format);
            writer.Write(tile.UncompressedSize);
            writer.Write(tile.Data.Length);
            writer.Write(tile.Data.Span);
        }

        return stream.ToArray();
    }

    public static ScreenTileBatchMsg Deserialize(ReadOnlySpan<byte> span)
    {
        var msg = new ScreenTileBatchMsg();
        if (span.Length < 6) return msg;

        int offset = 0;
        msg.FrameId = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset, 4));
        offset += 4;

        ushort count = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(offset, 2));
        offset += 2;

        for (int i = 0; i < count; i++)
        {
            if (offset + 17 > span.Length) break;

            ushort tileX = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(offset, 2));
            offset += 2;
            ushort tileY = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(offset, 2));
            offset += 2;
            ushort tileW = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(offset, 2));
            offset += 2;
            ushort tileH = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(offset, 2));
            offset += 2;
            var format = (TileCompressionFormat)span[offset++];
            uint uncompressedSize = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset, 4));
            offset += 4;
            int dataLen = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, 4));
            offset += 4;

            if (offset + dataLen > span.Length) break;

            byte[] data = span.Slice(offset, dataLen).ToArray();
            offset += dataLen;

            msg.Tiles.Add(new ScreenTileFrameMsg
            {
                FrameId = msg.FrameId,
                TileX = tileX,
                TileY = tileY,
                TileWidth = tileW,
                TileHeight = tileH,
                Format = format,
                UncompressedSize = uncompressedSize,
                Data = data
            });
        }

        return msg;
    }
}
