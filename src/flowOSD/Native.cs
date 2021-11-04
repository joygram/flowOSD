/*  Copyright © 2021, Albert Akhmetov <akhmetov@live.com>   
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
namespace flowOSD
{
    using System;
    using System.Runtime.InteropServices;

    static class Native
    {
        public const int ERROR_SUCCESS = 0x0;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern UInt32 RegisterWindowMessage(string lpString);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        [DllImport("uxtheme.dll", EntryPoint = "#132")]
        public static extern bool ShouldAppsUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#138")]
        public static extern bool ShouldSystemUseDarkMode();
    }
}