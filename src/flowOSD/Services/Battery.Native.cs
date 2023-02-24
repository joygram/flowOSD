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
namespace flowOSD.Services;

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Api;
using Microsoft.Win32.SafeHandles;
using static Native;

partial class Battery
{
    private const uint IOCTL_BATTERY_QUERY_TAG = 0x294040;
    private const uint IOCTL_BATTERY_QUERY_INFORMATION = 0x294044;
    private const uint IOCTL_BATTERY_QUERY_STATUS = 0x29404C;

    private const uint BATTERY_SYSTEM_BATTERY = 0x80000000;

    private const int DIGCF_DEVICEINTERFACE = 0x00000010;
    private const int DIGCF_PRESENT = 0x00000002;
    private const int ERROR_INSUFFICIENT_BUFFER = 0x7A;
    private const int ERROR_NO_MORE_ITEMS = 0x103;

    private static Guid GUID_DEVICE_BATTERY = new(0x72631e54, 0x78A4, 0x11d0, 0xbc, 0xf7, 0x00, 0xaa, 0x00, 0xb7, 0xb3, 0x2a);
    private static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

    private enum BATTERY_QUERY_INFORMATION_LEVEL
    {
        BatteryInformation,
        BatteryGranularityInformation,
        BatteryTemperature,
        BatteryEstimatedTime,
        BatteryDeviceName,
        BatteryManufactureDate,
        BatteryManufactureName,
        BatteryUniqueID,
        BatterySerialNumber
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BATTERY_QUERY_INFORMATION
    {
        public uint BatteryTag;
        public BATTERY_QUERY_INFORMATION_LEVEL InformationLevel;
        public uint AtRate;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BATTERY_INFORMATION
    {
        public uint Capabilities;
        public byte Technology;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public char[] Chemistry;

        public uint DesignedCapacity;
        public uint FullChargedCapacity;
        public uint DefaultAlert1;
        public uint DefaultAlert2;
        public uint CriticalBias;
        public uint CycleCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BATTERY_WAIT_STATUS
    {
        public uint BatteryTag;
        public uint Timeout;
        public uint PowerState;
        public uint LowCapacity;
        public uint HighCapacity;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BATTERY_STATUS
    {
        public uint PowerState;
        public uint Capacity;
        public uint Voltage;
        public int Rate;
    }

    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        int nInBufferSize,
        IntPtr lpOutBuffer,
        int nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static extern SafeFileHandle CreateFile(
        [MarshalAs(UnmanagedType.LPTStr)] string lpFileName,
        [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
        [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
        IntPtr lpSecurityAttributes,
        [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
        [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVICE_INTERFACE_DATA
    {
        public uint cbSize;
        public Guid InterfaceClassGuid;
        public uint Flags;
        public IntPtr Reserved;
    }

    [DllImport("SetupAPI.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr SetupDiGetClassDevs(
        ref Guid classGuid, 
        IntPtr enumerator, 
        IntPtr hwndParent, uint 
        flags);

    [DllImport("SetupAPI.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr deviceInfoSet, 
        IntPtr deviceInfoData, 
        ref Guid interfaceClassGuid, 
        uint MemberIndex, 
        ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

    [DllImport("SetupAPI.dll", SetLastError = true, EntryPoint = "SetupDiGetDeviceInterfaceDetailW", CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceInterfaceDetail
    (
        IntPtr deviceInfoSet,
        in SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
        [Out, Optional] IntPtr deviceInterfaceDetailData,
        uint deviceInterfaceDetailDataSize,
        out uint requiredSize,
        IntPtr deviceInfoData = default);

    [DllImport("SetupAPI.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);
}
