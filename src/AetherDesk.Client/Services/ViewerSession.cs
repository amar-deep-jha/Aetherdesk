using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Windows.Media.Imaging;
using AetherDesk.Core.Compression;
using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Core.Security;

namespace AetherDesk.Client.Services;

public sealed class ViewerSession : IDisposable
{
    private readonly string _partnerId;
    private readonly string _sessionId;
    private readonly EcdhKeyExchange _ecdh;
    private AesGcmCipher? _cipher;
    private ClientWebSocket? _relaySocket;
    private readonly CancellationTokenSource _cts = new();

    private int _width = 1920;
    private int _height = 1080;
    private byte[] _framebuffer = new byte[1920 * 1080 * 4];
    private readonly object _frameLock = new();

    private int _framesReceived;
    private long _bytesReceived;
    private long _lastFpsCalculationTime;
    public double CurrentFps { get; private set; }
    public double BandwidthKbps { get; private set; }
    public int Width => _width;
    public int Height => _height;
    public string PartnerId => _partnerId;
    public string SessionId => _sessionId;

    public event Action<int, int, int, int>? TileRendered; // x, y, w, h
    public event Action<int, int, int, int>? FrameReady; // minX, minY, width, height
    public event Action<int, int>? ResolutionChanged;
    public event Action<List<MonitorInfoMsg>>? MonitorListReceived;
    public event Action<string>? ClipboardReceived;
    public event Action<ChatMessageMsg>? ChatMessageReceived;
    public event Action<FileBrowseResponseMsg>? FileBrowseReceived;
    public event Action<FileTransferInitMsg>? FileTransferInitReceived;
    public event Action<FileTransferChunkMsg>? FileTransferChunkReceived;
    public event Action<FileTransferAckMsg>? FileTransferAckReceived;
    public event Action<string>? SessionDisconnected;

    public ViewerSession(string partnerId, string sessionId)
    {
        _partnerId = partnerId;
        _sessionId = sessionId;
        _ecdh = new EcdhKeyExchange();
        _lastFpsCalculationTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public void SetRemotePublicKey(string remotePublicKeyBase64)
    {
        byte[] sharedSecret = _ecdh.DeriveSharedKeyFromBase64(remotePublicKeyBase64);
        _cipher = new AesGcmCipher(sharedSecret);
    }

    public string GetLocalPublicKeyBase64() => _ecdh.PublicKeyBase64;

    public async Task ConnectRelayAsync(string relayWsUrl)
    {
        _relaySocket = new ClientWebSocket();
        await _relaySocket.ConnectAsync(new Uri(relayWsUrl), _cts.Token).ConfigureAwait(false);
        _ = Task.Run(ReceiveRelayLoopAsync);
    }

    public void CopyFrameRegion(int x, int y, int w, int h, byte[] destination, int destStride)
    {
        lock (_frameLock)
        {
            int srcStride = _width * 4;
            int copyBytesPerRow = w * 4;

            for (int row = 0; row < h; row++)
            {
                int srcOffset = (y + row) * srcStride + (x * 4);
                int dstOffset = row * destStride;
                if (srcOffset + copyBytesPerRow <= _framebuffer.Length && dstOffset + copyBytesPerRow <= destination.Length)
                {
                    Buffer.BlockCopy(_framebuffer, srcOffset, destination, dstOffset, copyBytesPerRow);
                }
            }
        }
    }

    private async Task ReceiveRelayLoopAsync()
    {
        byte[] receiveBuffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        using var stream = new MemoryStream();

        try
        {
            while (!_cts.Token.IsCancellationRequested && _relaySocket?.State == WebSocketState.Open)
            {
                var result = await _relaySocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), _cts.Token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.Count > 0)
                {
                    stream.Write(receiveBuffer, 0, result.Count);
                    _bytesReceived += result.Count;

                    if (result.EndOfMessage)
                    {
                        ProcessIncomingPacketBytes(stream.ToArray());
                        stream.SetLength(0);
                    }
                }
            }
        }
        catch { }
        finally
        {
            ArrayPool<byte>.Shared.Return(receiveBuffer);
            SessionDisconnected?.Invoke("Relay connection terminated.");
        }
    }

    public void ProcessIncomingPacketBytes(byte[] rawPacketBytes)
    {
        if (!AdbpPacket.TryDeserialize(rawPacketBytes, out var packet, out _) || packet == null)
            return;

        ReadOnlySpan<byte> payload = packet.Payload.Span;
        if (packet.Flags.HasFlag(AdbpFlags.Encrypted) && _cipher != null)
        {
            try
            {
                payload = _cipher.Decrypt(payload);
            }
            catch
            {
                return; // Decryption / auth tag failure
            }
        }

        switch (packet.OpCode)
        {
            case AdbpOpCode.ScreenResolution:
                var res = ScreenResolutionMsg.Deserialize(payload);
                if (res != null && res.Width > 0 && res.Height > 0)
                {
                    lock (_frameLock)
                    {
                        _width = res.Width;
                        _height = res.Height;
                        _framebuffer = new byte[_width * _height * 4];
                    }
                    ResolutionChanged?.Invoke(_width, _height);
                }
                break;

            case AdbpOpCode.ScreenTileBatch:
                var batch = ScreenTileBatchMsg.Deserialize(payload);
                if (batch.Tiles.Count > 0)
                {
                    int minX = int.MaxValue, minY = int.MaxValue;
                    int maxX = 0, maxY = 0;

                    lock (_frameLock)
                    {
                        int frameStride = _width * 4;
                        foreach (var t in batch.Tiles)
                        {
                            byte[] uncompressed = TileCompressor.DecompressTile(t.Data.Span, (int)t.UncompressedSize, t.Format);
                            int tileStride = t.TileWidth * 4;

                            for (int row = 0; row < t.TileHeight; row++)
                            {
                                int dstOffset = (t.TileY + row) * frameStride + (t.TileX * 4);
                                int srcOffset = row * tileStride;
                                if (dstOffset + tileStride <= _framebuffer.Length && srcOffset + tileStride <= uncompressed.Length)
                                {
                                    Buffer.BlockCopy(uncompressed, srcOffset, _framebuffer, dstOffset, tileStride);
                                }
                            }

                            if (t.TileX < minX) minX = t.TileX;
                            if (t.TileY < minY) minY = t.TileY;
                            if (t.TileX + t.TileWidth > maxX) maxX = t.TileX + t.TileWidth;
                            if (t.TileY + t.TileHeight > maxY) maxY = t.TileY + t.TileHeight;

                            TileRendered?.Invoke(t.TileX, t.TileY, t.TileWidth, t.TileHeight);
                        }
                    }

                    _framesReceived++;
                    UpdateTelemetryMetrics();

                    int dirtyW = Math.Min(_width - minX, maxX - minX);
                    int dirtyH = Math.Min(_height - minY, maxY - minY);
                    if (dirtyW > 0 && dirtyH > 0 && minX < _width && minY < _height)
                    {
                        FrameReady?.Invoke(minX, minY, dirtyW, dirtyH);
                    }
                }
                break;

            case AdbpOpCode.ScreenTileFrame:
                if (ScreenTileFrameMsg.TryDeserialize(payload, out var tile) && tile != null)
                {
                    byte[] uncompressed = TileCompressor.DecompressTile(tile.Data.Span, (int)tile.UncompressedSize, tile.Format);
                    lock (_frameLock)
                    {
                        int frameStride = _width * 4;
                        int tileStride = tile.TileWidth * 4;

                        for (int row = 0; row < tile.TileHeight; row++)
                        {
                            int dstOffset = (tile.TileY + row) * frameStride + (tile.TileX * 4);
                            int srcOffset = row * tileStride;
                            if (dstOffset + tileStride <= _framebuffer.Length && srcOffset + tileStride <= uncompressed.Length)
                            {
                                Buffer.BlockCopy(uncompressed, srcOffset, _framebuffer, dstOffset, tileStride);
                            }
                        }
                    }

                    _framesReceived++;
                    UpdateTelemetryMetrics();
                    TileRendered?.Invoke(tile.TileX, tile.TileY, tile.TileWidth, tile.TileHeight);
                }
                break;

            case AdbpOpCode.MonitorList:
                var monList = MonitorListMsg.Deserialize(payload);
                if (monList != null)
                {
                    MonitorListReceived?.Invoke(monList.Monitors);
                }
                break;

            case AdbpOpCode.ClipboardSync:
                var clip = ClipboardSyncMsg.Deserialize(payload);
                if (clip != null && !string.IsNullOrEmpty(clip.Text))
                {
                    ClipboardReceived?.Invoke(clip.Text);
                }
                break;

            case AdbpOpCode.ChatMessage:
                var chat = ChatMessageMsg.Deserialize(payload);
                if (chat != null)
                {
                    ChatMessageReceived?.Invoke(chat);
                }
                break;

            case AdbpOpCode.FileBrowseResponse:
                var browse = FileBrowseResponseMsg.Deserialize(payload);
                if (browse != null)
                {
                    FileBrowseReceived?.Invoke(browse);
                }
                break;

            case AdbpOpCode.FileTransferInit:
                var ftInit = FileTransferInitMsg.Deserialize(payload);
                if (ftInit != null)
                {
                    FileTransferInitReceived?.Invoke(ftInit);
                }
                break;

            case AdbpOpCode.FileTransferChunk:
                var ftChunk = FileTransferChunkMsg.Deserialize(payload);
                if (ftChunk != null)
                {
                    FileTransferChunkReceived?.Invoke(ftChunk);
                }
                break;

            case AdbpOpCode.FileTransferAck:
                var ftAck = FileTransferAckMsg.Deserialize(payload);
                if (ftAck != null)
                {
                    FileTransferAckReceived?.Invoke(ftAck);
                }
                break;

            case AdbpOpCode.DisconnectNotice:
                SessionDisconnected?.Invoke("Remote host terminated the session.");
                break;
        }
    }

    private void UpdateTelemetryMetrics()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long diff = now - _lastFpsCalculationTime;
        if (diff >= 1000)
        {
            CurrentFps = Math.Round((double)_framesReceived * 1000 / diff, 1);
            BandwidthKbps = Math.Round((double)_bytesReceived * 8 / diff, 1);
            _framesReceived = 0;
            _bytesReceived = 0;
            _lastFpsCalculationTime = now;
        }
    }

    public async Task SendPacketAsync(AdbpOpCode opCode, byte[] payload)
    {
        if (_relaySocket?.State != WebSocketState.Open) return;

        byte[] finalPayload = payload;
        AdbpFlags flags = AdbpFlags.None;

        if (_cipher != null)
        {
            finalPayload = _cipher.Encrypt(payload);
            flags |= AdbpFlags.Encrypted;
        }

        var packet = new AdbpPacket(opCode, finalPayload, flags);
        byte[] bytes = packet.Serialize();

        await _relaySocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
    }

    public Task SendMouseMoveAsync(int x, int y, bool isNormalized)
    {
        var msg = new MouseMoveMsg { X = x, Y = y, IsNormalized = isNormalized };
        return SendPacketAsync(AdbpOpCode.MouseMove, msg.Serialize());
    }

    public Task SendMouseButtonAsync(MouseButtonType btn, ButtonAction action, int x, int y)
    {
        var msg = new MouseButtonMsg { Button = btn, Action = action, X = x, Y = y };
        return SendPacketAsync(AdbpOpCode.MouseButton, msg.Serialize());
    }

    public Task SendMouseWheelAsync(int deltaX, int deltaY)
    {
        var msg = new MouseWheelMsg { DeltaX = deltaX, DeltaY = deltaY };
        return SendPacketAsync(AdbpOpCode.MouseWheel, msg.Serialize());
    }

    public Task SendKeyDownAsync(ushort vk, ushort sc, bool ext)
    {
        var msg = new KeyDownMsg { VirtualKey = vk, ScanCode = sc, Extended = ext };
        return SendPacketAsync(AdbpOpCode.KeyDown, msg.Serialize());
    }

    public Task SendKeyUpAsync(ushort vk, ushort sc, bool ext)
    {
        var msg = new KeyUpMsg { VirtualKey = vk, ScanCode = sc, Extended = ext };
        return SendPacketAsync(AdbpOpCode.KeyUp, msg.Serialize());
    }

    public Task SendChatMessageAsync(string text)
    {
        var msg = new ChatMessageMsg
        {
            SenderId = "VIEWER",
            SenderName = Environment.MachineName,
            Text = text,
            TimestampEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        return SendPacketAsync(AdbpOpCode.ChatMessage, msg.Serialize());
    }

    public Task RequestFileBrowseAsync(string path)
    {
        var msg = new FileBrowseRequestMsg { Path = path };
        return SendPacketAsync(AdbpOpCode.FileBrowseRequest, msg.Serialize());
    }

    public Task SendSystemCommandAsync(SystemCommandType command)
    {
        var msg = new SystemCommandMsg { Command = command };
        return SendPacketAsync(AdbpOpCode.SystemCommand, msg.Serialize());
    }

    public Task SelectMonitorAsync(int monitorIndex)
    {
        var msg = new SelectMonitorMsg { MonitorIndex = monitorIndex };
        return SendPacketAsync(AdbpOpCode.SelectMonitor, msg.Serialize());
    }

    public Task SendClipboardSyncAsync(string text)
    {
        var msg = new ClipboardSyncMsg { Text = text, Format = ClipboardFormatType.Text, TimestampEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
        return SendPacketAsync(AdbpOpCode.ClipboardSync, msg.Serialize());
    }

    public Task RequestKeyFrameAsync()
    {
        return SendPacketAsync(AdbpOpCode.KeyFrameRequest, Array.Empty<byte>());
    }

    public void Disconnect()
    {
        _cts.Cancel();
        try
        {
            _relaySocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Viewer disconnect", CancellationToken.None);
        }
        catch { }
    }

    public void Dispose()
    {
        Disconnect();
        _relaySocket?.Dispose();
        _ecdh.Dispose();
        _cipher?.Dispose();
        _cts.Dispose();
    }
}
