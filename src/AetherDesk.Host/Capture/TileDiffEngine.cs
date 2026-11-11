using AetherDesk.Core.Compression;
using AetherDesk.Core.Protocol.Messages;

namespace AetherDesk.Host.Capture;

/// <summary>
/// Splits screen frames into 64x64 tiles, detects modified regions via xxHash64,
/// and compresses only updated tiles for transmission.
/// </summary>
public sealed class TileDiffEngine
{
    public const int DefaultTileSize = 64;

    private readonly int _tileSize;
    private readonly byte[] _tileBuffer;
    private ulong[] _tileHashes = Array.Empty<ulong>();
    private int _tilesX;
    private int _tilesY;
    private int _currentWidth;
    private int _currentHeight;

    public int TileSize => _tileSize;

    public TileDiffEngine(int tileSize = DefaultTileSize)
    {
        _tileSize = tileSize;
        _tileBuffer = new byte[tileSize * tileSize * 4];
    }

    public void Reset()
    {
        Array.Clear(_tileHashes, 0, _tileHashes.Length);
    }

    /// <summary>
    /// Processes a full screen frame, compares against stored tile hashes, and returns compressed tile messages for all modified regions.
    /// </summary>
    public List<ScreenTileFrameMsg> ProcessFrame(CapturedFrame frame, bool forceKeyFrame = false, uint frameId = 0)
    {
        if (frame.Width != _currentWidth || frame.Height != _currentHeight)
        {
            _currentWidth = frame.Width;
            _currentHeight = frame.Height;
            _tilesX = (int)Math.Ceiling((double)_currentWidth / _tileSize);
            _tilesY = (int)Math.Ceiling((double)_currentHeight / _tileSize);
            _tileHashes = new ulong[_tilesX * _tilesY];
            forceKeyFrame = true;
        }

        var changedTiles = new List<ScreenTileFrameMsg>();
        byte[] frameBytes = frame.BgraBuffer;
        int frameStride = frame.Stride;

        for (int ty = 0; ty < _tilesY; ty++)
        {
            int startY = ty * _tileSize;
            int tileHeight = Math.Min(_tileSize, _currentHeight - startY);

            for (int tx = 0; tx < _tilesX; tx++)
            {
                int startX = tx * _tileSize;
                int tileWidth = Math.Min(_tileSize, _currentWidth - startX);
                int tileIndex = ty * _tilesX + tx;
                int tileStride = tileWidth * 4;
                int tileTotalBytes = tileStride * tileHeight;

                // Copy tile pixels line by line into contiguous tile buffer
                for (int row = 0; row < tileHeight; row++)
                {
                    int srcOffset = (startY + row) * frameStride + (startX * 4);
                    int dstOffset = row * tileStride;
                    Buffer.BlockCopy(frameBytes, srcOffset, _tileBuffer, dstOffset, tileStride);
                }

                ReadOnlySpan<byte> tileSpan = _tileBuffer.AsSpan(0, tileTotalBytes);
                ulong currentHash = TileHash.Compute(tileSpan);

                if (forceKeyFrame || currentHash != _tileHashes[tileIndex])
                {
                    _tileHashes[tileIndex] = currentHash;

                    byte[] compressed = TileCompressor.CompressTile(tileSpan, TileCompressionFormat.Deflate);

                    changedTiles.Add(new ScreenTileFrameMsg
                    {
                        FrameId = frameId,
                        MonitorIndex = 0,
                        Format = TileCompressionFormat.Deflate,
                        TileX = (ushort)startX,
                        TileY = (ushort)startY,
                        TileWidth = (ushort)tileWidth,
                        TileHeight = (ushort)tileHeight,
                        UncompressedSize = (uint)tileTotalBytes,
                        Data = compressed
                    });
                }
            }
        }

        return changedTiles;
    }
}
