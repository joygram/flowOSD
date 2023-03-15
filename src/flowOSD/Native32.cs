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
using Microsoft.Win32.SafeHandles;

static class Native32
{
    public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(0xffffffff);

    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint OPEN_EXISTING = 0x03;
    public const uint FILE_SHARE_READ = 1;
    public const uint FILE_SHARE_WRITE = 2;


    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool ReadFile(
        SafeHandle hFile,
        IntPtr lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        ref NativeOverlapped lpOverlapped);







    public const int S_OK = 0x00000000;
    public const int ERROR_SUCCESS = 0x0;



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




    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi)]
    internal static extern IntPtr LocalAlloc(LocalMemoryFlags uFlags, ulong uBytes);

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi)]
    internal static extern IntPtr LocalFree(IntPtr hMem);

  

    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();

}