# Digivista Remote — Scope & Features Specification

This document provides a precise, verified matrix of product capabilities delivered within the v1.1.0 engine release.

---

## 1. Core Feature Verification Matrix

| Feature Area | Engine Capability | Implementation Status | Implementation Verification |
| :--- | :--- | :--- | :--- |
| **Screen Capture** | DXGI Desktop Duplication API & fallback GDI screen capture | **Verified & Operational** | `GdiScreenCapturer.cs` & `MonitorManager.cs` capture 60 FPS framebuffers. |
| **Tile Diff Streaming** | 64x64 pixel grid differential hashing with adaptive compression | **Verified & Operational** | `TileDiffEngine.cs` isolates dirty rectangles; sends only modified tiles. |
| **Display Scaling** | Fit-to-window aspect ratio scaling and fullscreen toggle | **Verified & Operational** | Viewport preserves aspect ratio, letterboxing cleanly without distortion. |
| **Remote Mouse Control** | Relative and normalized mouse coordinates injected via Win32 `SendInput` | **Verified & Operational** | `WindowsInputInjector.InjectMouseMove` & `MouseButton` verified. |
| **Remote Keyboard Control** | Scan codes, virtual keys, and Unicode character injection | **Verified & Operational** | `WindowsInputInjector.InjectKeyDown` & `UnicodeChar` verified. |
| **Cursor Synchronization** | Host cursor shape (Arrow, Hand, I-Beam, Cross) rendering on viewport | **Verified & Operational** | `HostCursorManager.cs` tracks shape changes via `AdbpOpCode.CursorShape`. |
| **Attended Access** | Host prompts with interactive Accept/Decline and granular permission check | **Verified & Operational** | Incoming modal blocks connection until host explicitly approves. |
| **In-Session Host Control** | Floating host window with dynamic permission checkboxes & instant disconnect | **Verified & Operational** | `HostControlWindow.xaml` allows revoking mouse/keyboard/file in real time. |
| **Unattended Access** | Password-protected remote login with PBKDF2-HMAC-SHA256 (100k rounds) | **Verified & Operational** | Verified in `PasswordHasher.cs` and `HostSessionCoordinator.cs`. |
| **Clipboard Synchronization** | Bi-directional text clipboard synchronization with loop suppression | **Verified & Operational** | `ClipboardManager.cs` syncs text over encrypted `ClipboardSyncMsg`. |
| **File Transfer & Browser** | Directory browsing and 64KB chunked file transfer with SHA-256 verification | **Verified & Operational** | `FileTransferManager.cs` and `HostSessionCoordinator.cs` validated. |
| **In-Session Chat** | Real-time text messaging between viewer and host operator | **Verified & Operational** | In-session chat integrated in both `ViewerWindow` and `HostControlWindow`. |
| **Multi-Monitor Support** | Enumeration and dynamic switching between multiple connected displays | **Verified & Operational** | `MonitorManager.cs` broadcasts monitor lists; handles monitor selection. |
| **Signaling & Relay** | WebSocket RFC 6455 signaling and encrypted binary envelope relay | **Verified & Operational** | Node.js container deployed and verified on Railway. |
| **Security Audit Logging** | Embedded SQLite write-ahead logging (WAL) of all security events | **Verified & Operational** | `SecurityAuditLogger.cs` persists to `AuditLogs` table. |

---

## 2. Feature Boundary Qualification
Digivista Infotech's engineering team should note that all features above have been compiled, unit-tested, integration-tested, and verified over live local and relayed connections. Digivista Infotech should confirm acceptance against its own internal test environments before broad commercial distribution.

---

*Prepared for Digivista Infotech.*
