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
namespace flowOSD;

using System.Runtime.InteropServices;
using System.Security;

static class Native
{
    private const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;

    public const int S_OK = 0x00000000;
    public const int ERROR_SUCCESS = 0x0;

    public const int WM_WININICHANGE = 0x001A;
    public const int WM_DISPLAYCHANGE = 0x7E;
    public const int WM_DEVICECHANGE = 0x219;
    public const int WM_DPICHANGED = 0x02E0;
    public const int WM_DPICHANGED_BEFOREPARENT = 0x02E2;

    // https://learn.microsoft.com/en-us/windows/win32/inputdev/mouse-input-notifications

    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_LBUTTONDBLCLK = 0x0203;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP = 0x0205;
    public const int WM_RBUTTONDBLCLK = 0x0206;
    public const int WM_MBUTTONDOWN = 0x0207;
    public const int WM_MBUTTONUP = 0x0208;
    public const int WM_MBUTTONDBLCLK = 0x0209;

    public const int WM_MOUSEWHEEL = 0x020A;
    public const int WM_MOUSEHWHEEL = 0x020E;
    public const int WM_MOUSELEAVE = 0x02A3;
    public const int WM_MOUSEMOVE = 0x0200;


    public const int WM_CONTEXTMENU = 0x007B;

    [Flags]
    public enum LocalMemoryFlags : uint
    {
        LMEM_FIXED = 0x0000,
        LMEM_MOVEABLE = 0x0002,
        LMEM_NOCOMPACT = 0x0010,
        LMEM_NODISCARD = 0x0020,
        LMEM_ZEROINIT = 0x0040,
        LMEM_MODIFY = 0x0080,
        LMEM_DISCARDABLE = 0x0F00,
        LMEM_VALID_FLAGS = 0x0F72,
        LMEM_INVALID_HANDLE = 0x8000,
        LHND = (LMEM_MOVEABLE | LMEM_ZEROINIT),
        LPTR = (LMEM_FIXED | LMEM_ZEROINIT),
        NONZEROLHND = (LMEM_MOVEABLE),
        NONZEROLPTR = (LMEM_FIXED)
    }

    [DllImport("shlwapi.dll")]
    public static extern int ColorHLSToRGB(int hue, int luminance, int saturation);

    [DllImport("shlwapi.dll")]
    public static extern void ColorRGBToHLS(int rgb, out int hue, out int luminance, out int saturation);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi)]
    internal static extern IntPtr LocalAlloc(LocalMemoryFlags uFlags, ulong uBytes);

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi)]
    internal static extern IntPtr LocalFree(IntPtr hMem);

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, uint wMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern UInt32 RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    public static extern IntPtr GetCurrentThreadId();

    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();

    [DllImport("uxtheme.dll", EntryPoint = "#132")]
    public static extern bool ShouldAppsUseDarkMode();

    [DllImport("uxtheme.dll", EntryPoint = "#138")]
    public static extern bool ShouldSystemUseDarkMode();

    [DllImport("user32.dll")]
    public static extern int GetDpiForSystem();

    [DllImport("user32.dll")]
    public static extern int GetDpiForWindow(IntPtr hwnd);

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

    public static uint HiWord(IntPtr ptr)
    {
        var val = (uint)(int)ptr;
        if ((val & 0x80000000) == 0x80000000)
            return (val >> 16);
        else
            return (val >> 16) & 0xffff;
    }

    public static Point GetCursorPos()
    {
        var p = default(POINT);
        GetCursorPos(out p);

        return new Point(p.x, p.y);
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public int x;
        public int y;
    }

    public static void SetCornerPreference(IntPtr hWnd, DWM_WINDOW_CORNER_PREFERENCE cornerPreference)
    {
        var value = GCHandle.Alloc((uint)cornerPreference, GCHandleType.Pinned);
        var result = DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, value.AddrOfPinnedObject(), sizeof(uint));
        value.Free();
        if (result != 0)
        {
            throw Marshal.GetExceptionForHR(result);
        }

    }

    [SecurityCritical]
    [DllImport("dwmapi.dll", SetLastError = false, ExactSpelling = true)]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, uint dwAttribute, [In] IntPtr pvAttribute, int cbAttribute);

    public enum DWM_WINDOW_CORNER_PREFERENCE : uint
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }

    #region Acrylic

    public static void EnableAcrylic(IWin32Window window, Color blurColor)
    {
        if (window is null) throw new ArgumentNullException(nameof(window));

        var accentPolicy = new AccentPolicy
        {
            AccentState = ACCENT.ENABLE_ACRYLICBLURBEHIND,
            GradientColor = ToAbgr(blurColor)
        };

        unsafe
        {
            SetWindowCompositionAttribute(
                new HandleRef(window, window.Handle),
                new WindowCompositionAttributeData
                {
                    Attribute = WCA.ACCENT_POLICY,
                    Data = &accentPolicy,
                    DataLength = Marshal.SizeOf<AccentPolicy>()
                });
        }
    }

    private static uint ToAbgr(Color color)
    {
        return ((uint)color.A << 24)
            | ((uint)color.B << 16)
            | ((uint)color.G << 8)
            | color.R;
    }

    [DllImport("user32.dll")]
    private static extern int SetWindowCompositionAttribute(HandleRef hWnd, in WindowCompositionAttributeData data);

    private unsafe struct WindowCompositionAttributeData
    {
        public WCA Attribute;
        public void* Data;
        public int DataLength;
    }

    private enum WCA
    {
        ACCENT_POLICY = 19
    }

    private enum ACCENT
    {
        DISABLED = 0,
        ENABLE_GRADIENT = 1,
        ENABLE_TRANSPARENTGRADIENT = 2,
        ENABLE_BLURBEHIND = 3,
        ENABLE_ACRYLICBLURBEHIND = 4,
        INVALID_STATE = 5
    }

    private struct AccentPolicy
    {
        public ACCENT AccentState;
        public uint AccentFlags;
        public uint GradientColor;
        public uint AnimationId;
    }

    #endregion
}