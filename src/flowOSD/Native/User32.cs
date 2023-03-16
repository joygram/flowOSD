/*  Copyright © 2021-2023, Albert Akhmetov <akhmetov@live.com>   
 *
 *  This file is part of flowOSD.
 *
 *  flowOSD is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  flowOSD is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with flowOSD. If not, see <https://www.gnu.org/licenses/>.   
 *
 */

using System.Runtime.InteropServices;
using System.Text;

namespace flowOSD.Native;

static class User32
{
    public const int ENUM_CURRENT_SETTINGS = -1;

    public const int DISP_CHANGE_SUCCESSFUL = 0;
    public const int DISP_CHANGE_BADMODE = -2;
    public const int DISP_CHANGE_RESTART = 1;

    public const int DM_DISPLAYFREQUENCY = 0x400000;

    public const int CDS_UPDATEREGISTRY = 0x1;

    public delegate void WINEVENTPROC(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public UInt16 wVk;
        public UInt16 wScan;
        public UInt32 dwFlags;
        public UInt32 time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public Int32 dx;
        public Int32 dy;
        public UInt32 mouseData;
        public UInt32 dwFlags;
        public UInt32 time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public UInt32 uMsg;
        public UInt16 wParamL;
        public UInt16 wParamH;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT Mouse;
        [FieldOffset(0)] public KEYBDINPUT Keyboard;
        [FieldOffset(0)] public HARDWAREINPUT Hardware;
    }

    public struct INPUT
    {
        public UInt32 type;
        public INPUTUNION union;
    }

    [Flags]
    public enum KeyboardFlags
    {
        KEYEVENTF_KEYDOWN = 0x0000,
        KEYEVENTF_EXTENDEDKEY = 0x0001,
        KEYEVENTF_KEYUP = 0x0002,
        KEYEVENTF_UNICODE = 0x0004,
        KEYEVENTF_SCANCODE = 0x0008
    }

    [Flags]
    public enum InputType
    {
        INPUT_MOUSE = 0,
        INPUT_KEYBOARD = 1,
        INPUT_HARDWARE = 2
    }

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern uint SendInput(
        uint nInputs, 
        
        INPUT[] pInputs, int cbSize);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool GetLastInputInfo(
        ref LASTINPUTINFO plii);

    [DllImport(nameof(User32), CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool EnumDisplayDevices(
        string? lpDevice,
        uint iDevNum,
        ref DISPLAY_DEVICE lpDisplayDevice,
        uint dwFlags);

    [DllImport(nameof(User32), CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool EnumDisplaySettings(
        string lpszDeviceName,
        int iModeNum,
        ref DEVMODE lpDevMode);

    [DllImport(nameof(User32), CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int ChangeDisplaySettingsEx(
        string lpszDeviceName,
        ref DEVMODE lpDevMode,
        IntPtr hwnd,
        int dwFlags,
        IntPtr lParam);

    [DllImport(nameof(User32), CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr LoadImage(
        IntPtr hinst,
        string lpszName, 
        uint uType, 
        int cxDesired, 
        int cyDesired,
        uint fuLoad);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern int SendMessage(
        IntPtr hWnd, 
        uint wMsg, 
        IntPtr wParam, 
        IntPtr lParam);

    [DllImport(nameof(User32), CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int RegisterWindowMessage(
        string lpString);

    [DllImport(nameof(User32), CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetWindowThreadProcessId(
        IntPtr hWnd, 
        IntPtr lpdwProcessId);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool ShowWindow(
        IntPtr hWnd, 
        int nCmdShow);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool SetForegroundWindow(
        IntPtr hWnd);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool BringWindowToTop(
        IntPtr hWnd);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr SetActiveWindow(
        IntPtr hWnd);

    [DllImport(nameof(User32), SetLastError = true)]
    private static extern IntPtr FindWindowEx(
        IntPtr parentHandle, 
        IntPtr hWndChildAfter, 
        string className, 
        string windowTitle);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool AttachThreadInput(
        IntPtr idAttach, 
        IntPtr idAttachTo, 
        bool fAttach);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern int GetSystemMetrics(
        int nIndex);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr GetCurrentThreadId();

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern int GetDpiForSystem();

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern int GetDpiForWindow(
        IntPtr hwnd);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool GetCursorPos(
        out POINT lpPoint);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WINEVENTPROC lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport(nameof(User32), SetLastError = true)]
    public static extern bool UnhookWinEvent(
        IntPtr hWinEventHook);

    public static Point GetCursorPos()
    {
        GetCursorPos(out POINT p);

        return new Point(p.x, p.y);
    }

    public static void ShowAndActivate(IntPtr handle)
    {
        const int SW_SHOW = 1;

        var currentlyFocusedWindowProcessId = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
        var appThread = GetWindowThreadProcessId(handle, IntPtr.Zero);

        if (currentlyFocusedWindowProcessId != appThread)
        {
            AttachThreadInput(currentlyFocusedWindowProcessId, appThread, true);
            BringWindowToTop(handle);
            ShowWindow(handle, SW_SHOW);
            AttachThreadInput(currentlyFocusedWindowProcessId, appThread, false);
        }
        else
        {
            BringWindowToTop(handle);
            ShowWindow(handle, SW_SHOW);
        }
    }
}
