/*  Copyright © 2021-2022, Albert Akhmetov <akhmetov@live.com>   
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

static class Native
{
    public const int ERROR_SUCCESS = 0x0;

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
}