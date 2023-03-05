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
namespace flowOSD.Api;

using System.Runtime.InteropServices;
using System.Security;

partial class Icon
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateIconFromResourceEx(
        byte[] presbits,
        int dwResSize,
        bool fIcon,
        uint dwVer,
        int cxDesired,
        int cyDesired,
        uint Flags
    );

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private struct ICONDIR
    {
        public ushort idReserved;   // Reserved (must be 0)
        public ushort idType;       // Resource Type (1 for icons)
        public ushort idCount;      // How many images?
        public ICONDIRENTRY[] idEntries;   // An entry for each image (idCount of 'em)
    };

    private struct ICONDIRENTRY
    {
        public byte bWidth;          // Width, in pixels, of the image
        public byte bHeight;         // Height, in pixels, of the image
        public byte bColorCount;     // Number of colors in image (0 if >=8bpp)
        public byte bReserved;       // Reserved ( must be 0)
        public ushort wPlanes;       // Color Planes
        public ushort wBitCount;     // Bits per pixel
        public uint dwBytesInRes;    // How many bytes in this resource?
        public uint dwImageOffset;   // Where in the file is this image?
    };

}
