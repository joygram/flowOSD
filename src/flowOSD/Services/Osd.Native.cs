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
namespace flowOSD.Services;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

partial class Osd
{
    private static IntPtr GetSystemOsdHandle()
    {
        IntPtr hWndHost;
        while ((hWndHost = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "NativeHWNDHost", "")) != IntPtr.Zero)
        {
            IntPtr hWndDUI = FindWindowEx(hWndHost, IntPtr.Zero, "DirectUIHWND", "");
            if (hWndDUI != IntPtr.Zero)
            {
                GetWindowThreadProcessId(hWndHost, out int pid);
                if (Process.GetProcessById(pid).ProcessName.ToLower() == "explorer")
                {
                    return hWndHost;
                }
            }
        }

        return IntPtr.Zero;
    }

    private static string GetWindowClassName(IntPtr hWnd)
    {
        var className = new StringBuilder(256);
        int nRet = GetClassName(hWnd, className, className.Capacity);
        if (nRet != 0)
        {
            return className.ToString();
        }
        else
        {
            return string.Empty;
        }
    }

    private static int GetShellProcessId()
    {
        var hWndShell = GetShellWindow();
        GetWindowThreadProcessId(hWndShell, out int pid);

        return Process.GetProcessesByName("explorer").FirstOrDefault(p => p.Id == pid)?.Id ?? 0;
    }

    [DllImport("user32.dll", SetLastError = false)]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr hWndChildAfter, string className, string windowTitle);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, Int32 nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin,
        uint eventMax,
        IntPtr hmodWinEventProc,
        WINEVENTPROC lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    private delegate void WINEVENTPROC(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);
}