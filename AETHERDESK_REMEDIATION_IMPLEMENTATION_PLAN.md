# AetherDesk — Remediation Implementation Plan

**Document Version:** 1.0.0  
**Target Milestone:** Release 1.1.0-Production  
**Author:** Principal Software Architect & Lead Remote Desktop Engineer  

---

## 1. Objectives & Quality Gates

Transform AetherDesk into a commercial-grade, ultra-responsive remote desktop solution meeting or exceeding the user experience of AnyDesk and RustDesk across five critical vectors:
1. **Performance:** Smooth 60 FPS streaming, sub-50ms glass-to-glass latency, instantaneous keyboard/mouse responsiveness without stalls.
2. **True Full-Screen:** Immersive borderless full-screen, auto-hiding top overlay bar, multi-monitor selection, and flexible display scaling (Fit / 1:1 / Stretch).
3. **Robust Bidirectional File Transfer:** Dual-pane browser, upload/download, viewport drag-and-drop, SHA-256 chunk verification, and path traversal guards.
4. **Real-Time In-Session Chat:** Bidirectional chat between Viewer and Host with notifications, unread badges, and full Unicode support.
5. **Seamless Operation:** Clipboard sync, system key injection (Ctrl+Alt+Del, WinKey, Lock), and robust session recovery.

---

## 2. Execution Phases & Milestones

### Milestone 1: Core Performance Optimization (P0) — *COMPLETED & VERIFIED*
- Implement `ScreenTileBatchMsg` (OpCode `0x27`) to coalesce all dirty tiles into a single compressed, encrypted packet per frame.
- Refactor WPF viewport rendering from blocking `Dispatcher.Invoke` to asynchronous dirty-region `WriteableBitmap.WritePixels` via `Dispatcher.BeginInvoke(DispatcherPriority.Render)`.
- Apply 60 Hz rate limiting to mouse movement events to prevent socket flooding.
- Implement system commands (`InjectSystemCommand`: Ctrl+Alt+Del, WinKey, LockWorkstation, TaskManager).

### Milestone 2: True Full-Screen Remote Desktop (P0) — *IN PROGRESS*
- Add true borderless OS full-screen mode to `ViewerWindow`:
  - Toggle `WindowStyle.None`, `ResizeMode.NoResize`, and `WindowState.Maximized`.
  - Wire shortcuts: `F11`, `Ctrl+Alt+F`, and `Esc` to exit full-screen.
- Build auto-hiding overlay toolbar:
  - Slides down smoothly when cursor reaches the top border (`Y <= 6`) or when pinned.
  - Houses display controls, system keys, file manager, chat toggle, and disconnect button.
- Implement Display Scaling modes:
  - **Fit Screen:** Aspect-ratio preserving uniform scaling (default).
  - **1:1 Native Resolution:** Pixel-perfect remote rendering with scrollable/pannable viewport.
  - **Stretch / Fill:** Full viewport utilization.
- Add Remote Multi-Monitor Selector:
  - Discover remote monitors via `MonitorListMsg` / `SelectMonitorMsg`.
  - Allow switching between displays from the top toolbar.

### Milestone 3: Bidirectional File Transfer & Drag-and-Drop (P0) — *IN PROGRESS*
- In `HostSessionCoordinator`:
  - Add handlers for `AdbpOpCode.FileTransferInit`, `FileTransferChunk`, and `FileTransferCancel`.
  - Sanitize all file paths to prevent directory traversal outside allowed user directories.
  - Implement file chunk assembly and SHA-256 integrity validation upon completion.
  - Implement Host -> Viewer download streaming.
- In `ViewerWindow` & `FileTransferWindow`:
  - Enable viewport drag-and-drop: dropping files into the remote desktop initiates upload to remote Desktop.
  - Add "<- Download" button in `FileTransferWindow` to pull files from remote workstation.
  - Display real-time transfer progress, current speed (KB/s), bytes transferred, and status indicators.

### Milestone 4: In-Session Real-Time Chat (P0) — *IN PROGRESS*
- In `ViewerWindow`:
  - Ensure Chat Sidebar works seamlessly in both windowed and full-screen modes.
  - Add unread message indicator when sidebar is collapsed.
- In Host application:
  - Create `HostSessionBar` / floating in-session control widget when an incoming session is accepted.
  - Display live chat window on Host so host user can converse with remote technician in real time.
  - Provide one-click "End Session" button on the host desktop.

### Milestone 5: Clipboard Synchronization (P1) — *IN PROGRESS*
- Implement active clipboard listener/poller on both Host and Viewer.
- Transmit `ClipboardSyncMsg` (`0x60`) upon text change when `AllowClipboard` is enabled.
- Avoid echo loops by tracking last synchronized hash/text.

### Milestone 6: Quality Assurance, Verification & Packaging (P0) — *UPCOMING*
- Add automated tests in `AetherDesk.E2E.Tests` for batch streaming, full-screen toggling, file transfers, and chat.
- Execute full test suite (`dotnet test`) verifying 100% pass rate.
- Generate technical documentation (`BENCHMARKS.md`, `VERIFICATION_MATRIX.md`, `TEST_REPORT.md`, `KNOWN_LIMITATIONS.md`, `CHANGELOG.md`).
- Build self-contained Release distribution and package `AetherDesk-Partner-v1.0.0.zip`.
