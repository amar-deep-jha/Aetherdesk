# Digivista Remote — Access Control Architecture

## 1. Principles of Host Authorization
In Digivista Remote, the host computer maintains absolute authority over all remote interactions. No client possesses inherent rights to control or inspect a host machine without explicit attended authorization or cryptographic verification of unattended credentials.

---

## 2. Granular Permission Bitmask
Permissions are modeled via the `SessionPermissions` enumeration:

```csharp
[Flags]
public enum SessionPermissions : uint
{
    None = 0,
    AllowMouse = 1 << 0,        // Remote mouse movement, clicks, and wheel
    AllowKeyboard = 1 << 1,     // Remote keyboard scan codes and Unicode characters
    AllowClipboard = 1 << 2,    // In-session clipboard synchronization
    AllowFileTransfer = 1 << 3, // Bi-directional file browsing and file transfer
    AllowAudio = 1 << 4,        // Audio streaming (reserved)
    All = AllowMouse | AllowKeyboard | AllowClipboard | AllowFileTransfer | AllowAudio
}
```

---

## 3. Enforcement Pipeline

```
[ Remote Inbound Packet ]
            │
            ▼
[ Decrypt with AES-256-GCM ]
            │
            ▼
[ Inspect OpCode against Host Permissions ]
            │
      ┌─────┴─────────────────────────┐
      │                               │
[ Permission Active ]       [ Permission Revoked ]
      │                               │
      ▼                               ▼
[ Inject Win32 Event / Disk ]  [ Drop Packet Immediately ]
                                      │
                                      ▼
                               [ Log to SecurityAuditLogger: ]
                               [ "SECURITY_POLICY_VIOLATION" ]
```

---

## 4. In-Session Revocation Workflow
1. The host operator unchecks any permission checkbox in `HostControlWindow`.
2. `HostSessionCoordinator.UpdatePermissions(newPerms)` instantly updates the local bitmask.
3. An encrypted `PermissionChangeMsg` (`0xA1`) is dispatched to the viewer viewport.
4. Future input or file packets arriving under revoked permissions are dropped immediately and logged to the SQLite `AuditLogs` table.

---

*Prepared for Digivista Infotech.*
