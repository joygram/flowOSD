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

    public const int ERROR_SUCCESS = 0x0;

    public const uint WM_DEVICECHANGE = 0x219;


    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern UInt32 RegisterWindowMessage(string lpString);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

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