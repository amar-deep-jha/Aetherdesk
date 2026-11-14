# AetherDesk — Official Changelog

All notable changes to the **AetherDesk** remote desktop application are documented in this file.

---

## [1.1.0-Production] — 2026-11-14

### 🚀 Core Performance & Streaming Remediations (P0)
- **Implemented `ScreenTileBatchMsg` (OpCode `0x27`):** Coalesces all dirty 64x64 tiles modified during a frame into a single compressed, encrypted packet, eliminating the 510-packet per frame storm and reducing network framing overhead by 98%.
- **Target FPS Increased to 60 FPS:** Reconfigured capture intervals to 16ms, delivering butter-smooth 60 FPS desktop streaming.
- **Asynchronous Viewport Rendering:** Replaced UI-blocking synchronous `Dispatcher.Invoke` with non-blocking dirty-region blitting via `Dispatcher.BeginInvoke(DispatcherPriority.Render)` on `WriteableBitmap`.
- **60 Hz Mouse Input Throttling:** Implemented rate-limiting window on mouse move events, preventing duplex socket congestion and eliminating keyboard event lag ("frame, key, frame...").
- **System Command Injection:** Added dedicated toolbar controls and Win32 hardware injection for `Ctrl+Alt+Del`, `WinKey`, `LockWorkstation`, and `TaskManager`.

### 🖥️ True Full-Screen Remote Desktop (P0)
- **True Borderless Full-Screen Mode:** Added immersive borderless maximized full-screen toggle using Win32 styles (`WindowStyle = None`, `ResizeMode = NoResize`, `WindowState = Maximized`).
- **Keyboard Shortcuts:** Added `F11` and `Ctrl+Alt+F` to toggle full-screen, and `Esc` to instantly exit.
- **Auto-Hiding Top Toolbar:** Added top-edge hover trigger (`Height = 6`) that smoothly reveals the session controls when the cursor moves to the top edge and conceals them when moving away.
- **Toolbar Pinning:** Added a pin toggle (`📌`) allowing users to lock the toolbar open even in full-screen.
- **Display Scaling Modes:** Added 3-mode display cycle button:
  - `Fit Screen`: Aspect-ratio preserving uniform scaling.
  - `1:1 Native`: Pixel-perfect remote rendering with scrollable/pannable viewport.
  - `Stretch`: Full viewport utilization.
- **Multi-Monitor Remote Switcher:** Integrated `MonitorManager` discovery and `SelectMonitorMsg` dropdown on the viewer toolbar to switch captured displays on the fly.

### 📁 Bidirectional File Transfer & Drag-and-Drop (P0)
- **Host-Side File Transfer Engine:** Implemented handlers for `AdbpOpCode.FileTransferInit`, `FileTransferChunk`, and `FileTransferCancel` in `HostSessionCoordinator`, resolving the critical bug where file chunks were previously dropped by the host.
- **Bidirectional Transfers:** Added "Upload ->" (Local to Remote) and "&lt;- Download" (Remote to Local) actions with dual-pane directory explorer.
- **Viewport Drag-and-Drop:** Enabled dropping files directly from Windows Explorer onto the remote viewport to automatically upload them to the remote desktop with real-time overlay progress.
- **SHA-256 Integrity Validation:** Added full file hashing at sender and recipient with automatic deletion of corrupted transfers and integrity acknowledgment (`FileTransferAckMsg`).
- **Path Traversal Security:** Enforced strict filename sanitization and boundary validation to prevent directory traversal exploits.
- **Transfer Cancellation & Live Telemetry:** Added transfer progress bars, cancellation support, and dynamic speed (KB/s) calculation.

### 💬 In-Session Real-Time Text Chat (P0)
- **Host-Side Floating Control Window (`HostControlWindow`):** Added a sleek, top-right floating session widget for the host machine featuring live session indicators, permission badges, and full in-session chat.
- **Bidirectional Real-Time Communication:** Enabled seamless conversation between remote technician and local operator.
- **Chat Notification Badges:** Added unread message counters on the Viewer "Chat" button with automatic highlighting when the sidebar is collapsed.
- **Full Unicode Support:** Added support for multi-lingual chat (Hindi, Japanese, emojis) with formatted timestamps (`HH:mm:ss`).

### 📋 Clipboard Synchronization (P1)
- **Bidirectional Copy-Paste:** Implemented background STA clipboard monitors on both Host and Viewer.
- **Echo Loop Prevention:** Implemented last-synced hash tracking to prevent infinite clipboard echo loops.
- **Permission Gating:** Clipboard synchronization strictly adheres to `SessionPermissions.AllowClipboard`.

### 🛡️ Reliability & Security
- **Strict E2EE Enforcement:** Ephemeral ECDH X25519 key negotiation with AES-256-GCM authenticated cipher.
- **Luhn-9 Checksum Identifiers:** Validates device IDs with Luhn algorithm to prevent connection mistakes.
- **Graceful Session Teardown:** Immediate socket cleanup, resource disposal, and disconnect notices.

---

## [1.0.0-Release] — 2026-11-12
- Initial production baseline release.
