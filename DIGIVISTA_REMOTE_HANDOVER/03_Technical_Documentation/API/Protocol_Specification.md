# Digivista Remote — ADBP Binary Protocol Specification

## 1. Frame Structure
Communication between Viewer and Host occurs over the **AetherDesk Binary Protocol (ADBP)**.

Every packet consists of a fixed **16-byte Header** followed by a variable-length **Payload**:

```
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|       Magic (0xADBE)          |    Version    |    OpCode     |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|     Flags     |                   Reserved                    |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                        Sequence Number                        |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                        Payload Length                         |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                     Payload Data (N Bytes) ...                |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
```

### Header Fields:
- **Magic (2 Bytes):** `0xADBE` (`ADBP_MAGIC`). Packets lacking this magic number are immediately rejected.
- **Version (1 Byte):** `0x01` (Current protocol version).
- **OpCode (1 Byte):** Operation identifier specifying packet contents.
- **Flags (1 Byte):** Bitmask flags:
  - `0x01`: `Compressed`
  - `0x02`: `Encrypted` (AES-256-GCM ciphertext)
  - `0x04`: `KeyFrame`
  - `0x08`: `HighPriority`
- **Reserved (3 Bytes):** Reserved for 64-bit alignment and future extensions.
- **Sequence (4 Bytes):** Monotonically increasing packet sequence counter for replay mitigation.
- **Payload Length (4 Bytes):** Length of payload in bytes (maximum 16 MB).

---

## 2. Operation Codes (OpCodes)

| OpCode | Hex | Name | Payload Structure |
| :--- | :--- | :--- | :--- |
| **Handshake** | `0x01` | `HandshakeRequest` | DeviceId, EphemeralPublicKeyBase64 |
| | `0x02` | `HandshakeResponse` | Status, SessionId, EphemeralPublicKeyBase64 |
| | `0x03` | `AuthChallenge` | Salt, Iterations, Nonce |
| | `0x04` | `AuthResponse` | HashedCredentialToken |
| | `0x05` | `AuthResult` | Success (bool), GrantedPermissions |
| **Keepalive** | `0x10` | `Ping` | TimestampEpochMs (long) |
| | `0x11` | `Pong` | TimestampEpochMs (long) |
| **Display** | `0x20` | `ScreenResolution` | MonitorIndex, Width, Height, Dpi |
| | `0x22` | `CursorPosition` | X, Y, Visible |
| | `0x23` | `CursorShape` | CursorType, Width, Height, HotX, HotY, RgbaBytes |
| | `0x24` | `KeyFrameRequest` | ReasonCode |
| | `0x25` | `MonitorList` | Array of MonitorInfo |
| | `0x26` | `SelectMonitor` | MonitorIndex |
| | `0x27` | `ScreenTileBatch` | FrameNumber, TilesCount, CompressedBatchBytes |
| **Input** | `0x40` | `MouseMove` | X, Y, IsNormalized |
| | `0x41` | `MouseButton` | Button (Left/Right/Middle), Action (Down/Up/Click), X, Y |
| | `0x42` | `MouseWheel` | DeltaX, DeltaY |
| | `0x43` | `KeyDown` | VirtualKey, ScanCode, Extended |
| | `0x44` | `KeyUp` | VirtualKey, ScanCode, Extended |
| | `0x45` | `UnicodeChar` | UTF-16 Character |
| | `0x46` | `SystemCommand`| Command (Lock, CtrlAltDel) |
| **Clipboard** | `0x60` | `ClipboardSync` | Text |
| **File Transfer** | `0x70` | `FileBrowseRequest` | TargetPath |
| | `0x71` | `FileBrowseResponse`| DirectoryItemsArray |
| | `0x72` | `FileTransferInit` | TransferId, FileName, TotalSize, Sha256Hash |
| | `0x73` | `FileTransferChunk`| TransferId, Offset, ChunkBytes |
| | `0x74` | `FileTransferAck` | TransferId, Success, ErrorMessage |
| **Chat** | `0x90` | `ChatMessage` | MessageId, SenderId, SenderName, Text, Timestamp |
| **Session** | `0xA0` | `SessionControl` | Action (Disconnect, Lock), Reason |
| | `0xA1` | `PermissionChange` | Updated SessionPermissions Bitmask |
| | `0xAF` | `DisconnectNotice` | Reason |

---

*Prepared for Digivista Infotech.*
