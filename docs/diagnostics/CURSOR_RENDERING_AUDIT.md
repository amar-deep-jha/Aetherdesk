# AETHERDESK — CURSOR RENDERING & SYNCHRONIZATION AUDIT
**Document ID:** AD-AUDIT-CURS-001  
**Target:** Cursor capture, shape detection, network serialization, and client rendering  
**Classification:** Engineering Diagnostic & Remediation Audit  

---

## 1. Executive Summary

A critical user-reported defect in AetherDesk is:  
*The remote mouse pointer appears as a `+` symbol (crosshair) instead of the actual pointer (arrow, text beam, hand, etc.).*

Our investigation identified a two-fold root cause:
1. **Explicit Hardcoded Crosshair Cursor in XAML**: In `ViewerWindow.xaml` line 59, the `Image` control contains `Cursor="Cross"`. Whenever the mouse hovers over the remote screen viewport, WPF unconditionally overrides the cursor to a crosshair (`+`).
2. **Missing Host Cursor Pipeline**: The host engine uses Win32 `BitBlt` (`CAPTUREBLT`), which by default omits hardware cursor overlays. Furthermore, `HostSessionCoordinator.cs` never queries `GetCursorInfo` or dispatches `AdbpOpCode.CursorShape` or `AdbpOpCode.CursorPosition` messages, leaving the client completely unaware of remote cursor states.

---

## 2. Root-Cause Analysis

### 2.1 Viewer Hardcoded Cursor
```xml
<!-- ViewerWindow.xaml (Line 59) -->
<Image x:Name="ImgViewport" Stretch="Uniform"
       MouseMove="ImgViewport_MouseMove"
       MouseDown="ImgViewport_MouseDown"
       MouseUp="ImgViewport_MouseUp"
       MouseWheel="ImgViewport_MouseWheel"
       Cursor="Cross" <!-- ROOT CAUSE: Forces '+' symbol -->
       RenderOptions.BitmapScalingMode="HighQuality" />
```
Because `Cursor="Cross"` is declared on the element, the WPF framework forces the Win32 cursor shape to `IDC_CROSS` whenever the mouse enters the bounding box of `ImgViewport`.

### 2.2 Host Missing Cursor Extraction
In Windows, mouse pointers are rendered by the Desktop Window Manager (DWM) or GPU hardware overlay planes.
Win32 `BitBlt` captures the surface framebuffer without cursor graphics.
To properly support cursor rendering like AnyDesk or RustDesk:
1. **Host-Side Shape Poller**: The host must inspect the active cursor via Win32 `GetCursorInfo`:
   - Inspect `CURSORINFO.hCursor` and `CURSORINFO.flags & CURSOR_SHOWING`.
   - Identify standard system cursors by matching handles against cached system cursors (`LoadCursor(IntPtr.Zero, IDC_ARROW)`, `IDC_IBEAM`, `IDC_HAND`, etc.) or using `GetIconInfo`.
   - When the cursor shape or visibility changes, serialize a `CursorShapeMsg` and send via `AdbpOpCode.CursorShape`.
2. **Client-Side Dynamic Cursor**:
   - The client removes `Cursor="Cross"`.
   - Upon receiving `CursorShapeMsg`, the client updates `ImgViewport.Cursor` dynamically to `Cursors.Arrow`, `Cursors.IBeam`, `Cursors.Hand`, `Cursors.SizeAll`, `Cursors.Wait`, `Cursors.No`, etc.
   - For custom application cursors with RGBA bitmap data, generate a custom WPF `Cursor` using unmanaged memory or bitmap handle.

---

## 3. Cursor Shape Mapping Matrix

| Standard Cursor | Win32 Identifier | ADBP Shape Identifier | WPF Cursor |
| :--- | :--- | :--- | :--- |
| Normal Arrow | `IDC_ARROW (32512)` | `"Arrow"` | `Cursors.Arrow` |
| Text Select | `IDC_IBEAM (32513)` | `"IBeam"` | `Cursors.IBeam` |
| Link Select | `IDC_HAND (32649)` | `"Hand"` | `Cursors.Hand` |
| Busy / Wait | `IDC_WAIT (32514)` | `"Wait"` | `Cursors.Wait` |
| Working in Background | `IDC_APPSTARTING (32650)` | `"AppStarting"` | `Cursors.AppStarting` |
| Size All / Move | `IDC_SIZEALL (32646)` | `"SizeAll"` | `Cursors.SizeAll` |
| Size NS (Vertical) | `IDC_SIZENS (32645)` | `"SizeNS"` | `Cursors.SizeNS` |
| Size WE (Horizontal) | `IDC_SIZEWE (32644)` | `"SizeWE"` | `Cursors.SizeWE` |
| Size NWSE (Diagonal 1) | `IDC_SIZENWSE (32642)` | `"SizeNWSE"` | `Cursors.SizeNWSE` |
| Size NESW (Diagonal 2) | `IDC_SIZENESW (32643)` | `"SizeNESW"` | `Cursors.SizeNESW` |
| Unavailable / No | `IDC_NO (32648)` | `"No"` | `Cursors.No` |
| Precision Crosshair | `IDC_CROSS (32515)` | `"Cross"` | `Cursors.Cross` |
| Help Select | `IDC_HELP (32651)` | `"Help"` | `Cursors.Help` |

---

## 4. Remediation Steps

1. In `User32.cs`, add `CURSORINFO` struct, `GetCursorInfo`, `GetIconInfo`, `ICONINFO`, `DestroyIcon`.
2. Create `HostCursorManager.cs` in `AetherDesk.Host.Capture` to poll the cursor at 30-60 Hz and emit changes.
3. In `HostSessionCoordinator.cs`, integrate `HostCursorManager` and transmit `CursorShapeMsg` when changed.
4. In `ViewerSession.cs`, add handler and event for `AdbpOpCode.CursorShape` and `AdbpOpCode.CursorPosition`.
5. In `ViewerWindow.xaml`, change default `Cursor="Cross"` to `Cursor="Arrow"`.
6. In `ViewerWindow.xaml.cs`, subscribe to `CursorShapeReceived` and dynamically set `ImgViewport.Cursor`.
