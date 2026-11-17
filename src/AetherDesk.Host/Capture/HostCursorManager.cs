using System.Runtime.InteropServices;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Host.Native;

namespace AetherDesk.Host.Capture;

/// <summary>
/// Monitors and extracts remote Windows cursor shape and position
/// with Win32 hardware overlay cursor tracking.
/// </summary>
public sealed class HostCursorManager : IDisposable
{
    private IntPtr _hArrow = IntPtr.Zero;
    private IntPtr _hIBeam = IntPtr.Zero;
    private IntPtr _hWait = IntPtr.Zero;
    private IntPtr _hCross = IntPtr.Zero;
    private IntPtr _hSizeNWSE = IntPtr.Zero;
    private IntPtr _hSizeNESW = IntPtr.Zero;
    private IntPtr _hSizeWE = IntPtr.Zero;
    private IntPtr _hSizeNS = IntPtr.Zero;
    private IntPtr _hSizeAll = IntPtr.Zero;
    private IntPtr _hNo = IntPtr.Zero;
    private IntPtr _hHand = IntPtr.Zero;
    private IntPtr _hAppStarting = IntPtr.Zero;
    private IntPtr _hHelp = IntPtr.Zero;

    private IntPtr _lastHCursor = IntPtr.Zero;
    private string _lastShapeType = string.Empty;
    private int _lastX = -1;
    private int _lastY = -1;
    private bool _lastVisible = true;

    public event Action<CursorShapeMsg>? CursorShapeChanged;
    public event Action<CursorPositionMsg>? CursorPositionChanged;

    public HostCursorManager()
    {
        PreloadSystemCursors();
    }

    private void PreloadSystemCursors()
    {
        try
        {
            _hArrow = User32.LoadCursor(IntPtr.Zero, User32.IDC_ARROW);
            _hIBeam = User32.LoadCursor(IntPtr.Zero, User32.IDC_IBEAM);
            _hWait = User32.LoadCursor(IntPtr.Zero, User32.IDC_WAIT);
            _hCross = User32.LoadCursor(IntPtr.Zero, User32.IDC_CROSS);
            _hSizeNWSE = User32.LoadCursor(IntPtr.Zero, User32.IDC_SIZENWSE);
            _hSizeNESW = User32.LoadCursor(IntPtr.Zero, User32.IDC_SIZENESW);
            _hSizeWE = User32.LoadCursor(IntPtr.Zero, User32.IDC_SIZEWE);
            _hSizeNS = User32.LoadCursor(IntPtr.Zero, User32.IDC_SIZENS);
            _hSizeAll = User32.LoadCursor(IntPtr.Zero, User32.IDC_SIZEALL);
            _hNo = User32.LoadCursor(IntPtr.Zero, User32.IDC_NO);
            _hHand = User32.LoadCursor(IntPtr.Zero, User32.IDC_HAND);
            _hAppStarting = User32.LoadCursor(IntPtr.Zero, User32.IDC_APPSTARTING);
            _hHelp = User32.LoadCursor(IntPtr.Zero, User32.IDC_HELP);
        }
        catch { }
    }

    public void PollCursor()
    {
        var ci = new User32.CURSORINFO();
        ci.cbSize = Marshal.SizeOf<User32.CURSORINFO>();

        if (!User32.GetCursorInfo(ref ci))
        {
            return;
        }

        bool isVisible = (ci.flags & User32.CURSOR_SHOWING) != 0;
        if (isVisible != _lastVisible)
        {
            _lastVisible = isVisible;
            CursorPositionChanged?.Invoke(new CursorPositionMsg
            {
                X = ci.ptScreenPos.X,
                Y = ci.ptScreenPos.Y,
                Visible = isVisible
            });
        }

        if (!isVisible)
            return;

        // Position change check
        if (ci.ptScreenPos.X != _lastX || ci.ptScreenPos.Y != _lastY)
        {
            _lastX = ci.ptScreenPos.X;
            _lastY = ci.ptScreenPos.Y;
            CursorPositionChanged?.Invoke(new CursorPositionMsg
            {
                X = _lastX,
                Y = _lastY,
                Visible = true
            });
        }

        // Shape change check
        if (ci.hCursor != _lastHCursor)
        {
            _lastHCursor = ci.hCursor;
            string shapeType = ResolveCursorType(ci.hCursor, out int hotX, out int hotY);

            if (shapeType != _lastShapeType)
            {
                _lastShapeType = shapeType;
                CursorShapeChanged?.Invoke(new CursorShapeMsg
                {
                    CursorType = shapeType,
                    Width = 32,
                    Height = 32,
                    HotspotX = hotX,
                    HotspotY = hotY
                });
            }
        }
    }

    private string ResolveCursorType(IntPtr hCursor, out int hotX, out int hotY)
    {
        hotX = 0;
        hotY = 0;

        if (hCursor == IntPtr.Zero || hCursor == _hArrow)
            return "Arrow";
        if (hCursor == _hIBeam)
            return "IBeam";
        if (hCursor == _hHand)
            return "Hand";
        if (hCursor == _hWait)
            return "Wait";
        if (hCursor == _hAppStarting)
            return "AppStarting";
        if (hCursor == _hSizeAll)
            return "SizeAll";
        if (hCursor == _hSizeNS)
            return "SizeNS";
        if (hCursor == _hSizeWE)
            return "SizeWE";
        if (hCursor == _hSizeNESW)
            return "SizeNESW";
        if (hCursor == _hSizeNWSE)
            return "SizeNWSE";
        if (hCursor == _hNo)
            return "No";
        if (hCursor == _hCross)
            return "Cross";
        if (hCursor == _hHelp)
            return "Help";

        // Custom application cursor inspection
        try
        {
            if (User32.GetIconInfo(hCursor, out var info))
            {
                hotX = info.xHotspot;
                hotY = info.yHotspot;

                if (info.hbmMask != IntPtr.Zero)
                    Gdi32.DeleteObject(info.hbmMask);
                if (info.hbmColor != IntPtr.Zero)
                    Gdi32.DeleteObject(info.hbmColor);
            }
        }
        catch { }

        return "Arrow";
    }

    public void Reset()
    {
        _lastHCursor = IntPtr.Zero;
        _lastShapeType = string.Empty;
        _lastX = -1;
        _lastY = -1;
    }

    public void Dispose()
    {
        Reset();
    }
}
