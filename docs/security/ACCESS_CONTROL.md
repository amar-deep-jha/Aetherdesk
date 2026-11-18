# AetherDesk Access Control Specification

## 1. Principle of Least Privilege
Access control in AetherDesk is governed by the principle of explicit authorization. By default, incoming remote sessions have no inherent rights until explicitly approved by the local host operator or validated via unattended access credentials.

---

## 2. Permission Bitmask Model
Permissions are expressed using the `SessionPermissions` enumeration (`[Flags]` attribute):

```csharp
[Flags]
public enum SessionPermissions : uint
{
    None = 0,
    AllowMouse = 1 << 0,        // Remote cursor movement and click injection
    AllowKeyboard = 1 << 1,     // Remote keyboard scan codes and text injection
    AllowClipboard = 1 << 2,    // In-session clipboard synchronization
    AllowFileTransfer = 1 << 3, // Bi-directional file browsing and data chunk transfers
    AllowAudio = 1 << 4,        // Audio streaming (reserved for future release)
    All = AllowMouse | AllowKeyboard | AllowClipboard | AllowFileTransfer | AllowAudio
}
```

---

## 3. Enforcement Boundaries

### 3.1 Signaling Layer
The signaling server possesses zero permission logic. Permission negotiation and enforcement occur strictly between the two endpoint peers over the encrypted channel.

### 3.2 Host Protocol Layer
When an ADBP packet arrives at `HostSessionCoordinator.HandleIncomingPacket`:
1. The packet is decrypted using AES-256-GCM.
2. The packet opcode is inspected against active `Permissions`:
   - Mouse opcodes (`MouseMove`, `MouseButton`, `MouseWheel`) verify `AllowMouse`.
   - Keyboard opcodes (`KeyDown`, `KeyUp`, `UnicodeChar`, `SystemCommand`) verify `AllowKeyboard`.
   - Clipboard opcodes (`ClipboardSync`) verify `AllowClipboard`.
   - File transfer opcodes (`FileBrowseRequest`, `FileTransferInit`, `FileTransferChunk`) verify `AllowFileTransfer`.
3. If the required permission bit is not set:
   - The packet is dropped immediately.
   - An event is logged to `SecurityAuditLogger.LogPolicyViolation`.

### 3.3 Dynamic In-Session Revocation
- The Host operator can revoke permissions at any point during an active session via `HostControlWindow`.
- Changes take effect instantaneously on the Host thread and are broadcast to the Viewer via `PermissionChangeMsg`.

---

*Maintained by the AetherDesk Core Architecture & Engineering Team.*
