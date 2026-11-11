using System.IO;
using AetherDesk.Core.Identity;
using AetherDesk.Core.Protocol;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Core.Security;
using AetherDesk.Host.Capture;
using AetherDesk.Host.Clipboard;
using AetherDesk.Host.Input;

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
    private uint _frameCounter;
    private bool _requestKeyFrame = true;

    public HostSessionState State { get; private set; } = HostSessionState.Idle;
    public SessionPermissions Permissions { get; set; } = SessionPermissions.All;
    public string? PartnerDeviceId { get; private set; }
    public string SessionId { get; } = Guid.NewGuid().ToString("N");

    public event Action<AdbpPacket>? OutgoingPacketReady;
    public event Action<string, Action<bool, SessionPermissions>>? AttendedApprovalRequested;
    public event Action<ChatMessageMsg>? ChatMessageReceived;
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

        // Start capture loop
        _streamingTask = Task.Run(CaptureAndStreamLoopAsync);
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

            case AdbpOpCode.ClipboardSync:
                if (Permissions.HasFlag(SessionPermissions.AllowClipboard))
                {
                    var clip = ClipboardSyncMsg.Deserialize(payload);
                    if (clip != null && !string.IsNullOrEmpty(clip.Text))
                    {
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

            case AdbpOpCode.FileBrowseRequest:
                if (Permissions.HasFlag(SessionPermissions.AllowFileTransfer))
                {
                    HandleFileBrowse(payload);
                }
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

    private async Task CaptureAndStreamLoopAsync()
    {
        const int targetFps = 30;
        int frameIntervalMs = 1000 / targetFps;

        while (!_cts.Token.IsCancellationRequested && State == HostSessionState.Active)
        {
            var frameStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var frame = _capturer.CaptureFrame();
            if (frame != null)
            {
                uint frameId = unchecked(++_frameCounter);
                bool forceKey = _requestKeyFrame || (frameId % 150 == 0);
                if (forceKey)
                    _requestKeyFrame = false;

                var changedTiles = _diffEngine.ProcessFrame(frame, forceKey, frameId);

                foreach (var tile in changedTiles)
                {
                    byte[] tilePayload = tile.Serialize();
                    SendEncrypted(AdbpOpCode.ScreenTileFrame, tilePayload, AdbpFlags.Compressed);
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

    public void Dispose()
    {
        TerminateSession("Host disposed.");
        _capturer.Dispose();
        _ecdh.Dispose();
        _cipher?.Dispose();
        _cts.Dispose();
    }
}
