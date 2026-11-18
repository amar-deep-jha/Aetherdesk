# AETHERDESK — ACCESS CONTROL & PERMISSION GOVERNANCE AUDIT
**Document ID:** AD-AUDIT-ACC-001  
**Classification:** Security Architecture & Access Control Audit  
**Date:** November 18, 2026  
**Auditor:** Principal Security Architect  

---

## 1. Access Control Model Overview

AetherDesk's remote desktop access control follows the **Principle of Least Privilege (PoLP)** and **Explicit User Consent**.

Unlike simplistic screen-sharing tools that grant total remote control upon session initiation, AetherDesk isolates privileged remote capabilities into distinct, enforceable session permissions:

```csharp
[Flags]
public enum SessionPermissions : uint
{
    None = 0,
    AllowMouse = 1 << 0,         // Remote pointer moves, clicks, drag
    AllowKeyboard = 1 << 1,      // Keystroke injection and virtual keys
    AllowClipboard = 1 << 2,     // Bidirectional text clipboard synchronization
    AllowFileTransfer = 1 << 3,  // Uploading/downloading files
    AllowAudio = 1 << 4,         // Remote audio streaming
    All = AllowMouse | AllowKeyboard | AllowClipboard | AllowFileTransfer | AllowAudio
}
```

---

## 2. Enforcement Verification

### 2.1 Server & Host Enforcement (Non-Bypassable)
- **Mouse Injection Enforcement**: In `HostSessionCoordinator.cs`:
  ```csharp
  case AdbpOpCode.MouseMove:
  case AdbpOpCode.MouseButton:
  case AdbpOpCode.MouseWheel:
      if (Permissions.HasFlag(SessionPermissions.AllowMouse))
      {
          // Inject via WindowsInputInjector
      }
      // Dropped silently if permission is revoked or not granted
  ```
- **Keyboard Injection Enforcement**:
  ```csharp
  case AdbpOpCode.KeyDown:
  case AdbpOpCode.KeyUp:
  case AdbpOpCode.UnicodeChar:
  case AdbpOpCode.SystemCommand:
      if (Permissions.HasFlag(SessionPermissions.AllowKeyboard))
      {
          // Inject via WindowsInputInjector
      }
  ```
- **File Transfer Enforcement**:
  `FileBrowseRequest` and `FileTransferInit` packets check `Permissions.HasFlag(SessionPermissions.AllowFileTransfer)`. If false, an unauthorized error is immediately returned and no file handles are opened.
- **Clipboard Enforcement**:
  Incoming `ClipboardSync` packets are ignored unless `Permissions.HasFlag(SessionPermissions.AllowClipboard)`. Outbound host clipboard text is never captured or dispatched if clipboard permission is withheld.

---

## 3. Dynamic Revocation & Session Governance

- **In-Session Revocation**: The host user can revoke any permission mid-session via the floating `HostControlWindow`.
- **Immediate Propagation**: Revocation immediately updates `_coordinator.Permissions` and broadcasts `PermissionChangeMsg` to the viewer, disabling client controls in real time.
- **Session Termination**: Host maintains unilateral priority disconnect control (`TerminateSession`).
