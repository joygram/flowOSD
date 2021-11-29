namespace flowOSD.Services;

using System;
using System.Runtime.InteropServices;

partial class Display
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool EnumDisplayDevices(
        string lpDevice, 
        uint iDevNum, 
        ref DISPLAY_DEVICE lpDisplayDevice,
        uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern bool EnumDisplaySettings(
        string lpszDeviceName,
        int iModeNum,
        ref DEVMODE lpDevMode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int ChangeDisplaySettingsEx(
        string lpszDeviceName, 
        ref DEVMODE lpDevMode, 
        IntPtr hwnd, 
        int dwFlags, 
        IntPtr lParam);


    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Auto)]
    internal struct DEVMODE
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public UInt16 dmSpecVersion;
        public UInt16 dmDriverVersion;
        public UInt16 dmSize;
        public UInt16 dmDriverExtra;
        public UInt32 dmFields;

        public int dmPositionX;
        public int dmPositionY;
        public UInt32 dmDisplayOrientation;
        public UInt32 dmDisplayFixedOutput;

        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public UInt16 dmLogPixels;
        public UInt32 dmBitsPerPel;
        public UInt32 dmPelsWidth;
        public UInt32 dmPelsHeight;
        public UInt32 dmDisplayFlags;
        public UInt32 dmDisplayFrequency;
        public UInt32 dmICMMethod;
        public UInt32 dmICMIntent;
        public UInt32 dmMediaType;
        public UInt32 dmDitherType;
        public UInt32 dmReserved1;
        public UInt32 dmReserved2;
        public UInt32 dmPanningWidth;
        public UInt32 dmPanningHeight;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAY_DEVICE
    {
        public int cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
       
        public int StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }
}
