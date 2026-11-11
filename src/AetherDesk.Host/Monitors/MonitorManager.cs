using System.Runtime.InteropServices;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Host.Native;

namespace AetherDesk.Host.Monitors;

/// <summary>
/// Discovers and translates multi-display desktop configurations.
/// </summary>
public static class MonitorManager
{
    public static List<MonitorInfoMsg> EnumerateMonitors()
    {
        var monitors = new List<MonitorInfoMsg>();
        int index = 0;

        User32.MonitorEnumProc proc = (IntPtr hMonitor, IntPtr hdcMonitor, ref User32.RECT lprcMonitor, IntPtr dwData) =>
        {
            var mi = new User32.MONITORINFOEX();
            mi.cbSize = Marshal.SizeOf<User32.MONITORINFOEX>();

            if (User32.GetMonitorInfo(hMonitor, ref mi))
            {
                int width = mi.rcMonitor.Right - mi.rcMonitor.Left;
                int height = mi.rcMonitor.Bottom - mi.rcMonitor.Top;
                bool isPrimary = (mi.dwFlags & 1) != 0;

                monitors.Add(new MonitorInfoMsg
                {
                    Index = index++,
                    DeviceName = mi.szDevice ?? $"Display {index}",
                    Width = width,
                    Height = height,
                    Left = mi.rcMonitor.Left,
                    Top = mi.rcMonitor.Top,
                    IsPrimary = isPrimary
                });
            }
            return true;
        };

        User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);

        if (monitors.Count == 0)
        {
            // Fallback for headless environments
            monitors.Add(new MonitorInfoMsg
            {
                Index = 0,
                DeviceName = "Primary Display",
                Width = 1920,
                Height = 1080,
                Left = 0,
                Top = 0,
                IsPrimary = true
            });
        }

        return monitors;
    }
}
