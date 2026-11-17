# AETHERDESK — REMOTE DISPLAY & SCALING TEST PLAN
**Document ID:** AD-TEST-DISP-001  
**Category:** Quality Assurance & Functional Test Specification  

---

## 1. Scope & Objective

This test plan defines verification procedures for AetherDesk remote display scaling, aspect ratio preservation, coordinate accuracy, and cursor shape synchronization.

---

## 2. Test Cases

### TC-DISP-01: Fit to Window Aspect Ratio Preservation
- **Preconditions**: Remote host running at 1920x1080 (16:9). Viewer opened in 1024x768 window (4:3).
- **Procedure**:
  1. Observe initial rendering upon session connect.
  2. Resize viewer window horizontally and vertically.
- **Expected Outcome**:
  - Entire remote desktop visible without cropping.
  - Image centered horizontally with black pillarbox or letterbox margins.
  - No text stretching or aspect ratio distortion.

### TC-DISP-02: Fill Screen Mode
- **Procedure**:
  1. Click "Fit to Window" button to switch to "Fill Screen".
- **Expected Outcome**:
  - Image fills entire available viewport area without scrollbars.
  - Aspect ratio maintained (edges cropped appropriately).

### TC-DISP-03: Original Size (1:1 Native)
- **Procedure**:
  1. Switch mode to "Original (1:1)".
- **Expected Outcome**:
  - Image rendered at exact 1:1 remote pixel dimensions.
  - Scrollbars appear enabling horizontal and vertical panning.

### TC-DISP-04: Full-Screen Mode (F11)
- **Procedure**:
  1. Press `F11` or click "Fullscreen (F11)".
  2. Move mouse to top edge of monitor.
  3. Move mouse away from top edge.
  4. Press `Escape` or `F11`.
- **Expected Outcome**:
  - Viewer window covers entire monitor without window borders or taskbar.
  - Top toolbar auto-hides when mouse leaves top edge.
  - Hovering top edge smoothly reveals toolbar.
  - `Escape` or `F11` cleanly restores previous window state without reconnecting.

### TC-DISP-05: Coordinate Precision & Corner Click Mapping
- **Procedure**:
  1. In "Fit to Window" mode, click top-left pixel (0,0).
  2. Click bottom-right pixel of remote desktop.
  3. Click center button in a remote dialog.
- **Expected Outcome**:
  - Input events land on exact remote pixel.
  - Zero coordinate drift regardless of letterboxing margins.

### TC-DISP-06: Dynamic Cursor Shape Synchronization
- **Procedure**:
  1. Hover remote mouse over Notepad text area.
  2. Hover over a web hyperlink or button.
  3. Hover over a window resize border.
- **Expected Outcome**:
  - Viewer cursor instantly switches to `IBeam`.
  - Swapping over link switches to `Hand`.
  - Swapping over border switches to `SizeWE` or `SizeNS`.
  - Crosshair (`+`) is NEVER shown as default fallback.
