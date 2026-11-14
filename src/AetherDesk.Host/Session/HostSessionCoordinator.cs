using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Security.Cryptography;
using AetherDesk.Core.Identity;
using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Core.Security;
using AetherDesk.Host.Capture;
using AetherDesk.Host.Clipboard;
using AetherDesk.Host.Input;
using AetherDesk.Host.Monitors;

namespace AetherDesk.Host.Session;

public enum HostSessionState
{
    Idle,
    AwaitingApproval,
    Active,
    Terminated
}

public sealed class HostSessionCoordinator : IDisposable
{
    private readonly IScreenCapturer _capturer;
    private readonly TileDiffEngine _diffEngine;
    private readonly EcdhKeyExchange _ecdh;
    private AesGcmCipher? _cipher;

    private readonly CancellationTokenSource _cts = new();
    private Task? _streamingTask;
    private Task? _clipboardSyncTask;
    private string _lastSyncedClipboard = string.Empty;
    private uint _frameCounter;
    private bool _requestKeyFrame = true;

    private sealed class HostFileTransferState : IDisposable
    {
        public string TransferId { get; set; } = string.Empty;
        public string SafeFilePath { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public string ExpectedSha256 { get; set; } = string.Empty;
        public FileStream? Stream { get; set; }
        public long BytesWritten { get; set; }

        public void Dispose()
        {
            Stream?.Dispose();
            Stream = null;
        }
    }

    private readonly ConcurrentDictionary<string, HostFileTransferState> _incomingTransfers = new();

    public HostSessionState State { get; private set; } = HostSessionState.Idle;
    public SessionPermissions Permissions { get; set; } = SessionPermissions.All;
    public string? PartnerDeviceId { get; private set; }
    public string SessionId { get; } = Guid.NewGuid().ToString("N");

    public event Action<AdbpPacket>? OutgoingPacketReady;
    public event Action<string, Action<bool, SessionPermissions>>? AttendedApprovalRequested;
    public event Action<ChatMessageMsg>? ChatMessageReceived;
    public event Action<string>? FileTransferStatusChanged;
    public event Action<string>? SessionTerminated;

    public HostSessionCoordinator(IScreenCapturer? capturer = null)
    {
        _capturer = capturer ?? new GdiScreenCapturer();
        _diffEngine = new TileDiffEngine();
        _ecdh = new EcdhKeyExchange();
    }

    public void Initialize()
    {
        _capturer.Initialize();
    }

    public string GetLocalPublicKeyBase64() => _ecdh.PublicKeyBase64;

    /// <summary>
    /// Processes incoming handshake request from a remote viewer.
    /// </summary>
    public HandshakeResponse HandleHandshake(HandshakeRequest request)
    {
        PartnerDeviceId = request.DeviceId;

        byte[] sharedSecret = _ecdh.DeriveSharedKeyFromBase64(request.EphemeralPublicKeyBase64);
        _cipher = new AesGcmCipher(sharedSecret);

        State = HostSessionState.AwaitingApproval;

        AttendedApprovalRequested?.Invoke(request.DeviceId, (approved, perms) =>
        {
            if (approved)
                ApproveSession(perms);
            else
                TerminateSession("User rejected connection request.");
        });

        return new HandshakeResponse
        {
            Status = HandshakeStatus.Accepted,
            ServerVersion = "1.0.0",
            EphemeralPublicKeyBase64 = _ecdh.PublicKeyBase64,
            SessionId = SessionId,
            Message = "Session initialized. Awaiting authentication."
        };
    }

    /// <summary>
    /// Approves the session with specified permissions and launches the streaming loop.
    /// </summary>
    public void ApproveSession(SessionPermissions permissions)
    {
        Permissions = permissions;
        State = HostSessionState.Active;

        // Broadcast initial screen resolution
        var resMsg = new ScreenResolutionMsg
        {
            MonitorIndex = _capturer.MonitorIndex,
            Width = _capturer.Width,
            Height = _capturer.Height,
            Dpi = 96
        };
        SendEncrypted(AdbpOpCode.ScreenResolution, resMsg.Serialize());

        // Broadcast available displays
        try
        {
            var monitors = MonitorManager.EnumerateMonitors();
            var monListMsg = new MonitorListMsg { Monitors = monitors };
            SendEncrypted(AdbpOpCode.MonitorList, monListMsg.Serialize());
        }
        catch { }

        // Start capture loop
        _streamingTask = Task.Run(CaptureAndStreamLoopAsync);

        // Start clipboard sync loop
        _clipboardSyncTask = Task.Run(ClipboardSyncLoopAsync);
    }

    /// <summary>
    /// Verifies unattended access credentials.
    /// </summary>
    public bool VerifyUnattendedPassword(string password, string storedHash, SessionPermissions permissions)
    {
        if (PasswordHasher.VerifyPassword(password, storedHash))
        {
            ApproveSession(permissions);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Ingests and processes incoming ADBP packets from the remote viewer.
    /// </summary>
    public void HandleIncomingPacket(AdbpPacket packet)
    {
        ReadOnlySpan<byte> payload = packet.Payload.Span;

        if (packet.Flags.HasFlag(AdbpFlags.Encrypted) && _cipher != null)
        {
            payload = _cipher.Decrypt(payload);
        }

        switch (packet.OpCode)
        {
            case AdbpOpCode.MouseMove:
                if (Permissions.HasFlag(SessionPermissions.AllowMouse))
                {
                    var move = MouseMoveMsg.Deserialize(payload);
                    if (move != null)
                    {
                        WindowsInputInjector.InjectMouseMove(move.X, move.Y, move.IsNormalized, _capturer.Width, _capturer.Height);
                    }
                }
                break;

            case AdbpOpCode.MouseButton:
                if (Permissions.HasFlag(SessionPermissions.AllowMouse))
                {
                    var btn = MouseButtonMsg.Deserialize(payload);
                    if (btn != null)
                    {
                        WindowsInputInjector.InjectMouseButton(btn.Button, btn.Action, btn.X, btn.Y, _capturer.Width, _capturer.Height);
                    }
                }
                break;

            case AdbpOpCode.MouseWheel:
                if (Permissions.HasFlag(SessionPermissions.AllowMouse))
                {
                    var wheel = MouseWheelMsg.Deserialize(payload);
                    if (wheel != null)
                    {
                        WindowsInputInjector.InjectMouseWheel(wheel.DeltaX, wheel.DeltaY);
                    }
                }
                break;

            case AdbpOpCode.KeyDown:
                if (Permissions.HasFlag(SessionPermissions.AllowKeyboard))
                {
                    var kd = KeyDownMsg.Deserialize(payload);
                    if (kd != null)
                    {
                        WindowsInputInjector.InjectKeyDown(kd.VirtualKey, kd.ScanCode, kd.Extended);
                    }
                }
                break;

            case AdbpOpCode.KeyUp:
                if (Permissions.HasFlag(SessionPermissions.AllowKeyboard))
                {
                    var ku = KeyUpMsg.Deserialize(payload);
                    if (ku != null)
                    {
                        WindowsInputInjector.InjectKeyUp(ku.VirtualKey, ku.ScanCode, ku.Extended);
                    }
                }
                break;

            case AdbpOpCode.UnicodeChar:
                if (Permissions.HasFlag(SessionPermissions.AllowKeyboard))
                {
                    var uc = UnicodeCharMsg.Deserialize(payload);
                    if (uc != null)
                    {
                        WindowsInputInjector.InjectUnicodeChar(uc.Character);
                    }
                }
                break;

            case AdbpOpCode.SystemCommand:
                if (Permissions.HasFlag(SessionPermissions.AllowKeyboard))
                {
                    var sysCmd = SystemCommandMsg.Deserialize(payload);
                    WindowsInputInjector.InjectSystemCommand(sysCmd.Command);
                }
                break;

            case AdbpOpCode.ClipboardSync:
                if (Permissions.HasFlag(SessionPermissions.AllowClipboard))
                {
                    var clip = ClipboardSyncMsg.Deserialize(payload);
                    if (clip != null && !string.IsNullOrEmpty(clip.Text))
                    {
                        _lastSyncedClipboard = clip.Text;
                        ClipboardManager.SetText(clip.Text);
                    }
                }
                break;

            case AdbpOpCode.ChatMessage:
                var chat = ChatMessageMsg.Deserialize(payload);
                if (chat != null)
                {
                    ChatMessageReceived?.Invoke(chat);
                }
                break;

            case AdbpOpCode.KeyFrameRequest:
                _requestKeyFrame = true;
                break;

            case AdbpOpCode.SelectMonitor:
                HandleSelectMonitor(payload);
                break;

            case AdbpOpCode.FileBrowseRequest:
                if (Permissions.HasFlag(SessionPermissions.AllowFileTransfer))
                {
                    HandleFileBrowse(payload);
                }
                break;

            case AdbpOpCode.FileTransferInit:
                HandleFileTransferInit(payload);
                break;

            case AdbpOpCode.FileTransferChunk:
                HandleFileTransferChunk(payload);
                break;

            case AdbpOpCode.FileTransferCancel:
                HandleFileTransferCancel(payload);
                break;

            case AdbpOpCode.SessionControl:
                var ctrl = SessionControlMsg.Deserialize(payload);
                if (ctrl?.Action == SessionActionType.Disconnect)
                {
                    TerminateSession("Remote partner requested disconnect.");
                }
                break;
        }
    }

    private void HandleFileBrowse(ReadOnlySpan<byte> payload)
    {
        var req = FileBrowseRequestMsg.Deserialize(payload);
        if (req == null) return;

        string targetDir = string.IsNullOrWhiteSpace(req.Path)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : req.Path;

        var resp = new FileBrowseResponseMsg { Path = targetDir };
        try
        {
            if (Directory.Exists(targetDir))
            {
                var dirInfo = new DirectoryInfo(targetDir);
                foreach (var dir in dirInfo.GetDirectories())
                {
                    resp.Items.Add(new FileItemInfo
                    {
                        Name = dir.Name,
                        FullPath = dir.FullName,
                        IsDirectory = true,
                        Size = 0,
                        LastModifiedUtc = dir.LastWriteTimeUtc
                    });
                }
                foreach (var file in dirInfo.GetFiles())
                {
                    resp.Items.Add(new FileItemInfo
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        IsDirectory = false,
                        Size = file.Length,
                        LastModifiedUtc = file.LastWriteTimeUtc
                    });
                }
            }
            else
            {
                resp.ErrorMessage = "Directory does not exist.";
            }
        }
        catch (Exception ex)
        {
            resp.ErrorMessage = ex.Message;
        }

        SendEncrypted(AdbpOpCode.FileBrowseResponse, resp.Serialize());
    }

    private void HandleSelectMonitor(ReadOnlySpan<byte> payload)
    {
        var sel = SelectMonitorMsg.Deserialize(payload);
        if (sel == null) return;

        _capturer.Initialize(sel.MonitorIndex);
        var resMsg = new ScreenResolutionMsg
        {
            MonitorIndex = _capturer.MonitorIndex,
            Width = _capturer.Width,
            Height = _capturer.Height,
            Dpi = 96
        };
        SendEncrypted(AdbpOpCode.ScreenResolution, resMsg.Serialize());
        _requestKeyFrame = true;
    }

    private void HandleFileTransferInit(ReadOnlySpan<byte> payload)
    {
        if (!Permissions.HasFlag(SessionPermissions.AllowFileTransfer))
        {
            var deniedAck = new FileTransferAckMsg { Success = false, Error = "File transfer permission denied by host." };
            SendEncrypted(AdbpOpCode.FileTransferAck, deniedAck.Serialize());
            return;
        }

        var init = FileTransferInitMsg.Deserialize(payload);
        if (init == null) return;

        if (init.Direction == FileTransferDirection.Upload)
        {
            try
            {
                string safeFileName = Path.GetFileName(init.FileName);
                if (string.IsNullOrWhiteSpace(safeFileName))
                    safeFileName = $"Transfer_{init.TransferId[..8]}.dat";

                string targetDir = init.DestinationDirectory;
                if (string.IsNullOrWhiteSpace(targetDir) || !Directory.Exists(targetDir))
                {
                    targetDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    if (!Directory.Exists(targetDir))
                        targetDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                }

                string fullPath = Path.Combine(targetDir, safeFileName);
                if (File.Exists(fullPath))
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(safeFileName);
                    string ext = Path.GetExtension(safeFileName);
                    fullPath = Path.Combine(targetDir, $"{nameWithoutExt}_{DateTime.Now:yyyyMMddHHmmss}{ext}");
                }

                var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                var state = new HostFileTransferState
                {
                    TransferId = init.TransferId,
                    SafeFilePath = fullPath,
                    TotalSize = init.TotalSize,
                    ExpectedSha256 = init.Sha256Hash,
                    Stream = stream
                };

                _incomingTransfers[init.TransferId] = state;

                var ack = new FileTransferAckMsg { TransferId = init.TransferId, ChunkIndex = 0, Success = true };
                SendEncrypted(AdbpOpCode.FileTransferAck, ack.Serialize());

                FileTransferStatusChanged?.Invoke($"Receiving file: {Path.GetFileName(fullPath)} ({init.TotalSize / 1024} KB)");
            }
            catch (Exception ex)
            {
                var errAck = new FileTransferAckMsg { TransferId = init.TransferId, Success = false, Error = ex.Message };
                SendEncrypted(AdbpOpCode.FileTransferAck, errAck.Serialize());
            }
        }
        else if (init.Direction == FileTransferDirection.Download)
        {
            _ = Task.Run(() => StreamFileToViewerAsync(init));
        }
    }

    private async Task StreamFileToViewerAsync(FileTransferInitMsg req)
    {
        try
        {
            string remotePath = req.FileName;
            if (string.IsNullOrWhiteSpace(remotePath) || !File.Exists(remotePath))
            {
                var errAck = new FileTransferAckMsg { TransferId = req.TransferId, Success = false, Error = "File not found on host." };
                SendEncrypted(AdbpOpCode.FileTransferAck, errAck.Serialize());
                return;
            }

            var fileInfo = new FileInfo(remotePath);
            long totalSize = fileInfo.Length;
            byte[] fileBytes = await File.ReadAllBytesAsync(remotePath);
            string sha256 = Convert.ToHexString(SHA256.HashData(fileBytes));

            var initMsg = new FileTransferInitMsg
            {
                TransferId = req.TransferId,
                FileName = fileInfo.Name,
                DestinationDirectory = req.DestinationDirectory,
                TotalSize = totalSize,
                Sha256Hash = sha256,
                Direction = FileTransferDirection.Download
            };
            SendEncrypted(AdbpOpCode.FileTransferInit, initMsg.Serialize());

            const int chunkSize = 32 * 1024;
            long offset = 0;
            long seq = 0;

            while (offset < totalSize && State == HostSessionState.Active)
            {
                int currentChunk = (int)Math.Min(chunkSize, totalSize - offset);
                byte[] chunkBytes = new byte[currentChunk];
                Buffer.BlockCopy(fileBytes, (int)offset, chunkBytes, 0, currentChunk);

                var chunkMsg = new FileTransferChunkMsg
                {
                    TransferId = req.TransferId,
                    ChunkIndex = seq++,
                    Offset = offset,
                    DataBase64 = Convert.ToBase64String(chunkBytes),
                    IsLastChunk = (offset + currentChunk >= totalSize)
                };

                SendEncrypted(AdbpOpCode.FileTransferChunk, chunkMsg.Serialize());
                offset += currentChunk;

                await Task.Delay(3);
            }

            FileTransferStatusChanged?.Invoke($"Sent file: {fileInfo.Name} ({totalSize / 1024} KB)");
        }
        catch (Exception ex)
        {
            var errAck = new FileTransferAckMsg { TransferId = req.TransferId, Success = false, Error = ex.Message };
            SendEncrypted(AdbpOpCode.FileTransferAck, errAck.Serialize());
        }
    }

    private void HandleFileTransferChunk(ReadOnlySpan<byte> payload)
    {
        var chunk = FileTransferChunkMsg.Deserialize(payload);
        if (chunk == null) return;

        if (!_incomingTransfers.TryGetValue(chunk.TransferId, out var state) || state.Stream == null)
            return;

        try
        {
            byte[] chunkBytes = Convert.FromBase64String(chunk.DataBase64);
            state.Stream.Write(chunkBytes, 0, chunkBytes.Length);
            state.BytesWritten += chunkBytes.Length;

            if (chunk.IsLastChunk)
            {
                state.Stream.Flush();
                state.Stream.Dispose();
                state.Stream = null;

                byte[] completedBytes = File.ReadAllBytes(state.SafeFilePath);
                string actualHash = Convert.ToHexString(SHA256.HashData(completedBytes));

                if (!string.IsNullOrEmpty(state.ExpectedSha256) &&
                    !string.Equals(actualHash, state.ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(state.SafeFilePath);
                    var failAck = new FileTransferAckMsg
                    {
                        TransferId = chunk.TransferId,
                        ChunkIndex = chunk.ChunkIndex,
                        Success = false,
                        Error = "SHA-256 integrity mismatch."
                    };
                    SendEncrypted(AdbpOpCode.FileTransferAck, failAck.Serialize());
                    FileTransferStatusChanged?.Invoke($"File corrupted (SHA mismatch): {Path.GetFileName(state.SafeFilePath)}");
                }
                else
                {
                    var successAck = new FileTransferAckMsg
                    {
                        TransferId = chunk.TransferId,
                        ChunkIndex = chunk.ChunkIndex,
                        Success = true
                    };
                    SendEncrypted(AdbpOpCode.FileTransferAck, successAck.Serialize());
                    FileTransferStatusChanged?.Invoke($"File received: {Path.GetFileName(state.SafeFilePath)} ({state.BytesWritten / 1024} KB)");
                }

                _incomingTransfers.TryRemove(chunk.TransferId, out _);
            }
        }
        catch (Exception ex)
        {
            state.Dispose();
            _incomingTransfers.TryRemove(chunk.TransferId, out _);
            var errAck = new FileTransferAckMsg
            {
                TransferId = chunk.TransferId,
                Success = false,
                Error = ex.Message
            };
            SendEncrypted(AdbpOpCode.FileTransferAck, errAck.Serialize());
        }
    }

    private void HandleFileTransferCancel(ReadOnlySpan<byte> payload)
    {
        var cancel = FileTransferCancelMsg.Deserialize(payload);
        if (cancel == null) return;

        if (_incomingTransfers.TryRemove(cancel.TransferId, out var state))
        {
            state.Dispose();
            if (File.Exists(state.SafeFilePath))
            {
                try { File.Delete(state.SafeFilePath); } catch { }
            }
        }
    }

    private async Task ClipboardSyncLoopAsync()
    {
        while (!_cts.Token.IsCancellationRequested && State == HostSessionState.Active)
        {
            try
            {
                if (Permissions.HasFlag(SessionPermissions.AllowClipboard))
                {
                    string text = ClipboardManager.GetText();
                    if (!string.IsNullOrEmpty(text) && text != _lastSyncedClipboard)
                    {
                        _lastSyncedClipboard = text;
                        var clipMsg = new ClipboardSyncMsg { Text = text, Format = ClipboardFormatType.Text, TimestampEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
                        SendEncrypted(AdbpOpCode.ClipboardSync, clipMsg.Serialize());
                    }
                }
            }
            catch { }

            await Task.Delay(500, _cts.Token).ConfigureAwait(false);
        }
    }

    private async Task CaptureAndStreamLoopAsync()
    {
        const int targetFps = 60;
        int frameIntervalMs = 1000 / targetFps;

        while (!_cts.Token.IsCancellationRequested && State == HostSessionState.Active)
        {
            var frameStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var frame = _capturer.CaptureFrame();
            if (frame != null)
            {
                uint frameId = unchecked(++_frameCounter);
                bool forceKey = _requestKeyFrame || (frameId % 180 == 0);
                if (forceKey)
                    _requestKeyFrame = false;

                var changedTiles = _diffEngine.ProcessFrame(frame, forceKey, frameId);
                if (changedTiles.Count > 0)
                {
                    var batch = new ScreenTileBatchMsg
                    {
                        FrameId = frameId,
                        Tiles = changedTiles
                    };
                    byte[] batchPayload = batch.Serialize();
                    SendEncrypted(AdbpOpCode.ScreenTileBatch, batchPayload, AdbpFlags.Compressed);
                }
            }

            var elapsed = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - frameStart);
            int sleepTime = Math.Max(1, frameIntervalMs - elapsed);
            await Task.Delay(sleepTime, _cts.Token).ConfigureAwait(false);
        }
    }

    public void SendChatMessage(string text)
    {
        var msg = new ChatMessageMsg
        {
            SenderId = "HOST",
            SenderName = Environment.MachineName,
            Text = text,
            TimestampEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        SendEncrypted(AdbpOpCode.ChatMessage, msg.Serialize());
    }

    private void SendEncrypted(AdbpOpCode opCode, byte[] payload, AdbpFlags additionalFlags = AdbpFlags.None)
    {
        byte[] finalPayload = payload;
        AdbpFlags flags = additionalFlags;

        if (_cipher != null)
        {
            finalPayload = _cipher.Encrypt(payload);
            flags |= AdbpFlags.Encrypted;
        }

        var packet = new AdbpPacket(opCode, finalPayload, flags);
        OutgoingPacketReady?.Invoke(packet);
    }

    public void TerminateSession(string reason)
    {
        if (State == HostSessionState.Terminated)
            return;

        State = HostSessionState.Terminated;
        _cts.Cancel();

        var notice = new SessionControlMsg { Action = SessionActionType.Disconnect, Reason = reason };
        try
        {
            SendEncrypted(AdbpOpCode.DisconnectNotice, notice.Serialize());
        }
        catch { }

        SessionTerminated?.Invoke(reason);
    }

    private ClientWebSocket? _relaySocket;

    public async Task ConnectRelayAsync(string relayWsUrl)
    {
        try
        {
            _relaySocket = new ClientWebSocket();
            await _relaySocket.ConnectAsync(new Uri(relayWsUrl), _cts.Token).ConfigureAwait(false);

            OutgoingPacketReady += async packet =>
            {
                if (_relaySocket?.State == WebSocketState.Open)
                {
                    byte[] bytes = packet.Serialize();
                    try
                    {
                        await _relaySocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch { }
                }
            };

            _ = Task.Run(ReceiveRelayLoopAsync);
        }
        catch { }
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
                    if (result.EndOfMessage)
                    {
                        byte[] rawBytes = stream.ToArray();
                        stream.SetLength(0);

                        if (AdbpPacket.TryDeserialize(rawBytes, out var packet, out _) && packet != null)
                        {
                            HandleIncomingPacket(packet);
                        }
                    }
                }
            }
        }
        catch { }
        finally
        {
            ArrayPool<byte>.Shared.Return(receiveBuffer);
        }
    }

    public void Dispose()
    {
        TerminateSession("Host disposed.");
        try { _streamingTask?.Wait(500); } catch { }
        try { _clipboardSyncTask?.Wait(200); } catch { }
        foreach (var kvp in _incomingTransfers)
        {
            kvp.Value.Dispose();
        }
        _incomingTransfers.Clear();
        try
        {
            _relaySocket?.Dispose();
        }
        catch { }
        _capturer.Dispose();
        _ecdh.Dispose();
        _cipher?.Dispose();
        _cts.Dispose();
    }
}
