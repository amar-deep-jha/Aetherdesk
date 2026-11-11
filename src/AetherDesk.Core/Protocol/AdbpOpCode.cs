namespace AetherDesk.Core.Protocol;

/// <summary>
/// AetherDesk Binary Protocol (ADBP) operation codes.
/// </summary>
public enum AdbpOpCode : byte
{
    None = 0x00,

    // Handshake & Connection Negotiation (0x01 - 0x0F)
    HandshakeRequest = 0x01,
    HandshakeResponse = 0x02,
    AuthChallenge = 0x03,
    AuthResponse = 0x04,
    AuthResult = 0x05,

    // Keepalive & Telemetry (0x10 - 0x1F)
    Ping = 0x10,
    Pong = 0x11,
    BandwidthReport = 0x12,

    // Display & Video Streaming (0x20 - 0x3F)
    ScreenResolution = 0x20,
    ScreenTileFrame = 0x21,
    CursorPosition = 0x22,
    CursorShape = 0x23,
    KeyFrameRequest = 0x24,
    MonitorList = 0x25,
    SelectMonitor = 0x26,

    // User Input Injection (0x40 - 0x5F)
    MouseMove = 0x40,
    MouseButton = 0x41,
    MouseWheel = 0x42,
    KeyDown = 0x43,
    KeyUp = 0x44,
    UnicodeChar = 0x45,

    // Clipboard Synchronization (0x60 - 0x6F)
    ClipboardSync = 0x60,

    // File Transfer (0x70 - 0x8F)
    FileBrowseRequest = 0x70,
    FileBrowseResponse = 0x71,
    FileTransferInit = 0x72,
    FileTransferChunk = 0x73,
    FileTransferAck = 0x74,
    FileTransferCancel = 0x75,

    // Communication (0x90 - 0x9F)
    ChatMessage = 0x90,

    // Session Control & Permissions (0xA0 - 0xAF)
    SessionControl = 0xA0,
    PermissionChange = 0xA1,
    DisconnectNotice = 0xAF
}

[Flags]
public enum AdbpFlags : byte
{
    None = 0x00,
    Encrypted = 0x01,
    Compressed = 0x02,
    KeyFrame = 0x04,
    Relayed = 0x08,
    Acknowledged = 0x10
}
