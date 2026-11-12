using System.Runtime.InteropServices;
using AetherDesk.Core.Protocol.Messages;
using AetherDesk.Host.Native;

namespace AetherDesk.Host.Input;

/// <summary>
/// Injects hardware-level mouse movements, clicks, scrolling, and keyboard events using Win32 SendInput.
/// </summary>
public static class WindowsInputInjector
{
    private static readonly int InputSize = Marshal.SizeOf<User32.INPUT>();

    public static bool InjectMouseMove(int x, int y, bool isNormalized, int screenWidth, int screenHeight)
    {
        int normX = x;
        int normY = y;

        if (!isNormalized)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                screenWidth = User32.GetSystemMetrics(User32.SM_CXVIRTUALSCREEN);
                screenHeight = User32.GetSystemMetrics(User32.SM_CYVIRTUALSCREEN);
            }
            normX = (int)Math.Round((double)x * 65535 / Math.Max(1, screenWidth - 1));
            normY = (int)Math.Round((double)y * 65535 / Math.Max(1, screenHeight - 1));
        }

        var input = new User32.INPUT
        {
            type = User32.INPUT_MOUSE,
            u = new User32.INPUTUNION
            {
                mi = new User32.MOUSEINPUT
                {
                    dx = normX,
                    dy = normY,
                    dwFlags = User32.MOUSEEVENTF_MOVE | User32.MOUSEEVENTF_ABSOLUTE | User32.MOUSEEVENTF_VIRTUALDESK,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        return User32.SendInput(1, new[] { input }, InputSize) == 1;
    }

    public static bool InjectMouseButton(MouseButtonType button, ButtonAction action, int x, int y, int screenWidth, int screenHeight)
    {
        InjectMouseMove(x, y, true, screenWidth, screenHeight);

        uint downFlag = 0;
        uint upFlag = 0;
        uint mouseData = 0;

        switch (button)
        {
            case MouseButtonType.Left:
                downFlag = User32.MOUSEEVENTF_LEFTDOWN;
                upFlag = User32.MOUSEEVENTF_LEFTUP;
                break;
            case MouseButtonType.Right:
                downFlag = User32.MOUSEEVENTF_RIGHTDOWN;
                upFlag = User32.MOUSEEVENTF_RIGHTUP;
                break;
            case MouseButtonType.Middle:
                downFlag = User32.MOUSEEVENTF_MIDDLEDOWN;
                upFlag = User32.MOUSEEVENTF_MIDDLEUP;
                break;
            case MouseButtonType.XButton1:
                downFlag = User32.MOUSEEVENTF_XDOWN;
                upFlag = User32.MOUSEEVENTF_XUP;
                mouseData = 0x0001;
                break;
            case MouseButtonType.XButton2:
                downFlag = User32.MOUSEEVENTF_XDOWN;
                upFlag = User32.MOUSEEVENTF_XUP;
                mouseData = 0x0002;
                break;
        }

        var inputs = new List<User32.INPUT>();

        if (action == ButtonAction.Down || action == ButtonAction.Click || action == ButtonAction.DoubleClick)
        {
            inputs.Add(CreateMouseInput(downFlag, mouseData));
        }

        if (action == ButtonAction.Up || action == ButtonAction.Click || action == ButtonAction.DoubleClick)
        {
            inputs.Add(CreateMouseInput(upFlag, mouseData));
        }

        if (action == ButtonAction.DoubleClick)
        {
            inputs.Add(CreateMouseInput(downFlag, mouseData));
            inputs.Add(CreateMouseInput(upFlag, mouseData));
        }

        if (inputs.Count == 0)
            return false;

        return User32.SendInput((uint)inputs.Count, inputs.ToArray(), InputSize) == (uint)inputs.Count;
    }

    public static bool InjectMouseWheel(int deltaX, int deltaY)
    {
        var inputs = new List<User32.INPUT>();

        if (deltaY != 0)
        {
            inputs.Add(new User32.INPUT
            {
                type = User32.INPUT_MOUSE,
                u = new User32.INPUTUNION
                {
                    mi = new User32.MOUSEINPUT
                    {
                        dwFlags = User32.MOUSEEVENTF_WHEEL,
                        mouseData = unchecked((uint)deltaY),
                        time = 0,
                        dwExtraInfo = UIntPtr.Zero
                    }
                }
            });
        }

        if (deltaX != 0)
        {
            inputs.Add(new User32.INPUT
            {
                type = User32.INPUT_MOUSE,
                u = new User32.INPUTUNION
                {
                    mi = new User32.MOUSEINPUT
                    {
                        dwFlags = User32.MOUSEEVENTF_HWHEEL,
                        mouseData = unchecked((uint)deltaX),
                        time = 0,
                        dwExtraInfo = UIntPtr.Zero
                    }
                }
            });
        }

        if (inputs.Count == 0)
            return true;

        return User32.SendInput((uint)inputs.Count, inputs.ToArray(), InputSize) == (uint)inputs.Count;
    }

    public static bool InjectKeyDown(ushort virtualKey, ushort scanCode, bool extended)
    {
        uint flags = 0;
        if (extended)
            flags |= User32.KEYEVENTF_EXTENDEDKEY;
        if (scanCode != 0)
            flags |= User32.KEYEVENTF_SCANCODE;

        var input = new User32.INPUT
        {
            type = User32.INPUT_KEYBOARD,
            u = new User32.INPUTUNION
            {
                ki = new User32.KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        return User32.SendInput(1, new[] { input }, InputSize) == 1;
    }

    public static bool InjectKeyUp(ushort virtualKey, ushort scanCode, bool extended)
    {
        uint flags = User32.KEYEVENTF_KEYUP;
        if (extended)
            flags |= User32.KEYEVENTF_EXTENDEDKEY;
        if (scanCode != 0)
            flags |= User32.KEYEVENTF_SCANCODE;

        var input = new User32.INPUT
        {
            type = User32.INPUT_KEYBOARD,
            u = new User32.INPUTUNION
            {
                ki = new User32.KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        return User32.SendInput(1, new[] { input }, InputSize) == 1;
    }

    public static bool InjectUnicodeChar(char ch)
    {
        var down = new User32.INPUT
        {
            type = User32.INPUT_KEYBOARD,
            u = new User32.INPUTUNION
            {
                ki = new User32.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
                    dwFlags = User32.KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        var up = new User32.INPUT
        {
            type = User32.INPUT_KEYBOARD,
            u = new User32.INPUTUNION
            {
                ki = new User32.KEYBDINPUT
                {
                    wVk = 0,
                    wScan = ch,
                    dwFlags = User32.KEYEVENTF_UNICODE | User32.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        return User32.SendInput(2, new[] { down, up }, InputSize) == 2;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool LockWorkStation();

    public static bool InjectSystemCommand(SystemCommandType command)
    {
        switch (command)
        {
            case SystemCommandType.WinKey:
                InjectKeyDown(0x5B, 0, false);
                InjectKeyUp(0x5B, 0, false);
                return true;

            case SystemCommandType.LockWorkstation:
                return LockWorkStation();

            case SystemCommandType.TaskManager:
                InjectKeyDown(0x11, 0, false);
                InjectKeyDown(0x10, 0, false);
                InjectKeyDown(0x1B, 0, false);
                InjectKeyUp(0x1B, 0, false);
                InjectKeyUp(0x10, 0, false);
                InjectKeyUp(0x11, 0, false);
                return true;

            case SystemCommandType.CtrlAltDel:
                InjectKeyDown(0x11, 0, false);
                InjectKeyDown(0x12, 0, false);
                InjectKeyDown(0x2E, 0, true);
                InjectKeyUp(0x2E, 0, true);
                InjectKeyUp(0x12, 0, false);
                InjectKeyUp(0x11, 0, false);
                return true;

            default:
                return false;
        }
    }

    private static User32.INPUT CreateMouseInput(uint flags, uint mouseData = 0)
    {
        return new User32.INPUT
        {
            type = User32.INPUT_MOUSE,
            u = new User32.INPUTUNION
            {
                mi = new User32.MOUSEINPUT
                {
                    dwFlags = flags,
                    mouseData = mouseData,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };
    }
}
