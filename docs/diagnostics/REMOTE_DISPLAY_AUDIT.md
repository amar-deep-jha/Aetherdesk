# AETHERDESK — REMOTE DISPLAY SCALING & VIEWPORT AUDIT
**Document ID:** AD-AUDIT-DISP-001  
**Target:** Display pipeline, viewport sizing, aspect ratio management, and coordinate mapping  
**Classification:** Engineering Diagnostic & Remediation Audit  

---

## 1. Executive Summary

A thorough architectural audit of the AetherDesk client display pipeline revealed fundamental structural flaws in how remote desktop frames are sized, laid out, and mapped to user inputs in `AetherDesk.Client`.

The reported defect—*remote desktop does not properly fit the local viewer's screen, leaving awkward black borders or rendering at an unusable size*—is directly caused by two major issues:
1. **Hardcoded pixel dimensions on unconstrained layout elements**: `ViewerWindow.xaml.cs` explicitly assigns `ImgViewport.Width = width; ImgViewport.Height = height;` inside a `ScrollViewer`. Because `ScrollViewer` grants infinite measurement space, WPF bypasses `Stretch="Uniform"` scaling, causing the image to render at native pixel size rather than fitting the available window.
2. **Defective mouse coordinate normalization**: The input handler calculates normalized coordinates via `pos.X * 65535 / ImgViewport.ActualWidth` without accounting for letterbox/pillarbox margins or viewport offsets, introducing coordinate drift whenever the window aspect ratio differs from the remote monitor.

---

## 2. Source Code Inspection & Root-Cause Analysis

### 2.1 Viewport Container Layout (`ViewerWindow.xaml`)
```xml
<!-- Existing Code (Lines 52-60) -->
<ScrollViewer x:Name="SvViewport" Grid.Row="1" Grid.Column="0" HorizontalScrollBarVisibility="Auto" VerticalScrollBarVisibility="Auto" Background="#030712">
    <Grid Background="#030712" HorizontalAlignment="Center" VerticalAlignment="Center">
        <Image x:Name="ImgViewport" Stretch="Uniform"
               MouseMove="ImgViewport_MouseMove"
               MouseDown="ImgViewport_MouseDown"
               MouseUp="ImgViewport_MouseUp"
               MouseWheel="ImgViewport_MouseWheel"
               Cursor="Cross"
               RenderOptions.BitmapScalingMode="HighQuality" />
...
```
**Defects Identified:**
- `ScrollViewer` measures children with `AvailableSize = Size(PositiveInfinity, PositiveInfinity)`.
- When child `Image` has explicit `Width` and `Height`, it consumes those exact dimensions.
- `Stretch="Uniform"` has no effect because the element is allocated all the space it requested, forcing scrollbars instead of scaling to the client window.

### 2.2 Bitmap Initialization (`ViewerWindow.xaml.cs`)
```csharp
// Existing Code (Lines 70-79)
private void InitBitmap(int width, int height)
{
    Dispatcher.Invoke(() =>
    {
        _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        ImgViewport.Source = _bitmap;
        ImgViewport.Width = width;   // <-- CRITICAL DEFECT: Hardcoded explicit width
        ImgViewport.Height = height; // <-- CRITICAL DEFECT: Hardcoded explicit height
    });
}
```
**Defects Identified:**
- Explicitly assigning `ImgViewport.Width = width` (e.g. 1920) and `ImgViewport.Height = height` (e.g. 1080) locks the WPF element layout size.
- In "Fit to Window" mode, `ImgViewport.Width` and `ImgViewport.Height` must be `double.NaN` (auto) or dynamically computed based on the container size:
  $$\text{scale} = \min\left(\frac{W_{\text{avail}}}{W_{\text{remote}}}, \frac{H_{\text{avail}}}{H_{\text{remote}}}\right)$$

### 2.3 Mouse Coordinate Transformation (`ViewerWindow.xaml.cs`)
```csharp
// Existing Code (Lines 290-297)
private void ImgViewport_MouseMove(object sender, MouseEventArgs e)
{
    var pos = e.GetPosition(ImgViewport);
    if (_session.Width <= 0 || _session.Height <= 0 || ImgViewport.ActualWidth <= 0 || ImgViewport.ActualHeight <= 0) return;

    int normX = (int)Math.Clamp(Math.Round(pos.X * 65535 / ImgViewport.ActualWidth), 0, 65535);
    int normY = (int)Math.Clamp(Math.Round(pos.Y * 65535 / ImgViewport.ActualHeight), 0, 65535);
...
```
**Defects Identified:**
- When `Stretch="Uniform"` or `None` is active inside a container, `e.GetPosition(ImgViewport)` may encompass letterbox or pillarbox margins if `ImgViewport` stretches beyond the rendered bitmap aspect ratio.
- In 1:1 Native mode with scrollbars, `pos.X` is relative to `ImgViewport` but the host expects exact absolute or normalized coordinates matching the remote screen.
- Mathematical Inverse Transformation required:
  $$X_{\text{remote}} = \text{clamp}\left((X_{\text{local}} - X_{\text{offset}}) \times \frac{W_{\text{remote}}}{W_{\text{rendered}}}, 0, W_{\text{remote}} - 1\right)$$
  $$\text{normX} = \text{round}\left(X_{\text{remote}} \times \frac{65535}{W_{\text{remote}} - 1}\right)$$

---

## 3. Viewing Modes Specification & Architectural Remedies

| Mode | Aspect Ratio | Scaling Formula | Scrollbars | Centering |
| :--- | :--- | :--- | :--- | :--- |
| **Fit to Window (Default)** | Preserved | $S = \min(W_{\text{client}}/W_{\text{remote}}, H_{\text{client}}/H_{\text{remote}})$ | Disabled (`Hidden`) | Centered with letterbox/pillarbox bars |
| **Fill Screen** | Preserved | $S = \max(W_{\text{client}}/W_{\text{remote}}, H_{\text{client}}/H_{\text{remote}})$ | Disabled (`Hidden`) | Centered with controlled crop |
| **Original Size (1:1)** | Preserved | $S = 1.0$ | Enabled (`Auto`) | Origin (0,0) or centered when smaller |
| **Full-Screen (F11)** | Preserved | $S = \min(W_{\text{mon}}/W_{\text{remote}}, H_{\text{mon}}/H_{\text{remote}})$ | Hidden | Borderless, auto-hiding top toolbar |

---

## 4. Verification Strategy

1. **Unit & Calculation Tests**: Test scaling and inverse coordinate transform across 16:9, 16:10, 4:3, 21:9 ultra-wide, and portrait orientations.
2. **Visual Viewport Verification**: Ensure zero distortion, crisp font rendering (`RenderOptions.BitmapScalingMode="HighQuality"`), and smooth resize transitions.
3. **Input Alignment**: Verify that clicking the exact corners (0,0) and (W-1, H-1) translates to exact remote pixels.
