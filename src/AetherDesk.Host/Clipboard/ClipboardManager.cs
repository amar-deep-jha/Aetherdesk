using System.Runtime.InteropServices;
using AetherDesk.Host.Native;

namespace AetherDesk.Host.Clipboard;

/// <summary>
/// Safe clipboard manager for retrieving and setting clipboard unicode text.
/// </summary>
public static class ClipboardManager
{
    private static string _lastKnownText = string.Empty;

    public static string GetText()
    {
        string result = string.Empty;

        // Clipboard operations on Windows must run on STA thread
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Clipboard.ContainsText())
                {
                    result = System.Windows.Clipboard.GetText();
                }
            }
            catch
            {
                // Fallback to Win32 API if WPF Clipboard fails
                result = GetTextWin32();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(500);

        return result;
    }

    public static bool SetText(string text)
    {
        if (text == _lastKnownText)
            return true;

        _lastKnownText = text;
        bool success = false;

        var thread = new Thread(() =>
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
                success = true;
            }
            catch
            {
                success = SetTextWin32(text);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(500);

        return success;
    }

    private static string GetTextWin32()
    {
        if (!User32.OpenClipboard(IntPtr.Zero))
            return string.Empty;

        try
        {
            IntPtr handle = User32.GetClipboardData(User32.CF_UNICODETEXT);
            if (handle == IntPtr.Zero)
                return string.Empty;

            return Marshal.PtrToStringUni(handle) ?? string.Empty;
        }
        finally
        {
            User32.CloseClipboard();
        }
    }

    private static bool SetTextWin32(string text)
    {
        if (!User32.OpenClipboard(IntPtr.Zero))
            return false;

        try
        {
            User32.EmptyClipboard();
            IntPtr hGlobal = Marshal.StringToHGlobalUni(text);
            User32.SetClipboardData(User32.CF_UNICODETEXT, hGlobal);
            return true;
        }
        finally
        {
            User32.CloseClipboard();
        }
    }
}
