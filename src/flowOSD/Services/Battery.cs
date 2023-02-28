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

sealed partial class Battery : IDisposable, IBattery
{
    private SafeFileHandle batteryHandle;
    private uint batteryTag;

    private BehaviorSubject<int> rateSubject;
    private BehaviorSubject<uint> capacitySubject;
    private BehaviorSubject<BatteryPowerState> powerStateSubject;

    public Battery()
    {
        if (!Init())
        {
            throw new ApplicationException("Can't connect to the battery.");
        }

        var batteryStatus = GetBatteryStatus(batteryHandle, batteryTag);

        rateSubject = new BehaviorSubject<int>(batteryStatus.Rate);
        capacitySubject = new BehaviorSubject<uint>(batteryStatus.Capacity);
        powerStateSubject = new BehaviorSubject<BatteryPowerState>((BatteryPowerState)batteryStatus.PowerState);

        Rate = rateSubject.AsObservable();
        Capacity = capacitySubject.AsObservable();
        PowerState = powerStateSubject.AsObservable();
    }

    void IDisposable.Dispose()
    {
        if (batteryHandle != null)
        {
            batteryHandle.Dispose();
            batteryHandle = null;
        }
    }

    public string Name { get; private set; }

    public string ManufactureName { get; private set; }

    public uint DesignedCapacity { get; private set; }

    public uint FullChargedCapacity { get; private set; }

    public uint CycleCount { get; private set; }

    public IObservable<int> Rate { get; }

    public IObservable<uint> Capacity { get; }

    public IObservable<BatteryPowerState> PowerState { get; }

    public void Update()
    {
        if (!batteryHandle.IsInvalid || batteryHandle.IsClosed)
        {
            batteryHandle.Dispose();
            Init();
        }

        var batteryStatus = GetBatteryStatus(batteryHandle, batteryTag);

        rateSubject.OnNext(batteryStatus.Rate);
        capacitySubject.OnNext(batteryStatus.Capacity);
        powerStateSubject.OnNext((BatteryPowerState)batteryStatus.PowerState);
    }

    private bool Init()
    {
        var disHandle = SetupDiGetClassDevs(ref GUID_DEVICE_BATTERY, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
        if (disHandle == INVALID_HANDLE_VALUE)
        {
            return false;
        }

        try
        {
            uint i = 0;
            SP_DEVICE_INTERFACE_DATA? deviceInterfaceData;

            while ((deviceInterfaceData = GetDeviceInterfaceData(disHandle, i++)) != null)
            {
                string devicePath = GetDevicePath(disHandle, deviceInterfaceData.Value);

                SafeFileHandle battery = CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);

                if (!battery.IsInvalid)
                {
                    var batteryTag = GetBatteryTag(battery);
                    var batteryInformation = GetBatteryInformation(battery, batteryTag);

                    if (batteryInformation.Capabilities == BATTERY_SYSTEM_BATTERY)
                    {
                        Name = GetDeviceName(battery, batteryTag, BATTERY_QUERY_INFORMATION_LEVEL.BatteryDeviceName);
                        ManufactureName = GetDeviceName(battery, batteryTag, BATTERY_QUERY_INFORMATION_LEVEL.BatteryManufactureName);
                        DesignedCapacity = batteryInformation.DesignedCapacity;
                        FullChargedCapacity = batteryInformation.FullChargedCapacity;
                        CycleCount = batteryInformation.CycleCount;

                        if (Name == "ASUS Battery" && ManufactureName == "ASUSTeK")
                        {
                            this.batteryHandle = battery;
                            this.batteryTag = batteryTag;

                            return true;
                        }
                    }
                }

                battery.Dispose();
            }

        }
        finally
        {
            SetupDiDestroyDeviceInfoList(disHandle);
        }

        return false;
    }

    private SP_DEVICE_INTERFACE_DATA? GetDeviceInterfaceData(IntPtr hdev, uint index)
    {
        SP_DEVICE_INTERFACE_DATA did = default;
        did.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVICE_INTERFACE_DATA));

        if (SetupDiEnumDeviceInterfaces(hdev, IntPtr.Zero, ref GUID_DEVICE_BATTERY, index, ref did))
        {
            return did;
        }

        if (Marshal.GetLastWin32Error() == ERROR_NO_MORE_ITEMS)
        {
            return null;
        }
        else
        {
            throw new Win32Exception((int)GetLastError());
        }
    }

    private string GetDevicePath(IntPtr hdev, SP_DEVICE_INTERFACE_DATA did)
    {
        SetupDiGetDeviceInterfaceDetail(hdev, did, IntPtr.Zero, 0, out uint cbRequired, IntPtr.Zero);

        if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
        {
            var ptr = Marshal.AllocHGlobal((int)cbRequired);
            try
            {
                Marshal.WriteInt32(ptr, Environment.Is64BitOperatingSystem ? 8 : 4); // cbSize.

                if (SetupDiGetDeviceInterfaceDetail(hdev, did, ptr, cbRequired, out _, IntPtr.Zero))
                {
                    return Marshal.PtrToStringUni(ptr + 4);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        return null;
    }

    private static void DeviceIoControl(
        SafeFileHandle batteryHandle,
        uint controlCode,
        IntPtr inBuffer,
        int inBufferSize,
        IntPtr outBuffer,
        int outBufferSize)
    {
        var result = DeviceIoControl(
            batteryHandle,
            controlCode,
            inBuffer,
            inBufferSize,
            outBuffer,
            outBufferSize,
            out _,
            IntPtr.Zero);

        if (!result)
        {
            throw new Win32Exception((int)GetLastError());
        }
    }

    private static uint GetBatteryTag(SafeFileHandle batteryHandle)
    {
        var inBuffer = Marshal.AllocHGlobal(sizeof(uint));
        try
        {
            Marshal.WriteInt32(inBuffer, 0);

            var outBuffer = Marshal.AllocHGlobal(sizeof(uint));
            try
            {
                DeviceIoControl(
                    batteryHandle,
                    IOCTL_BATTERY_QUERY_TAG,
                    inBuffer,
                    sizeof(uint),
                    outBuffer,
                    sizeof(uint));

                return Marshal.PtrToStructure<uint>(outBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(inBuffer);
        }
    }

    private static BATTERY_INFORMATION GetBatteryInformation(SafeFileHandle batteryHandle, uint batteryTag)
    {
        BATTERY_QUERY_INFORMATION query = default;
        query.BatteryTag = batteryTag;
        query.InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryInformation;

        var inBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(query));
        try
        {
            var outBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(BATTERY_INFORMATION)));
            try
            {
                Marshal.StructureToPtr(query, inBuffer, false);

                DeviceIoControl(
                    batteryHandle,
                    IOCTL_BATTERY_QUERY_INFORMATION,
                    inBuffer,
                    Marshal.SizeOf(query),
                    outBuffer,
                    Marshal.SizeOf(typeof(BATTERY_INFORMATION)));

                return Marshal.PtrToStructure<BATTERY_INFORMATION>(outBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(inBuffer);
        }
    }

    private static string GetDeviceName(SafeFileHandle batteryHandle, uint batteryTag, BATTERY_QUERY_INFORMATION_LEVEL level)
    {
        const int maxLoadString = 100;

        BATTERY_QUERY_INFORMATION query = default;
        query.BatteryTag = batteryTag;
        query.InformationLevel = level;

        var inBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(query));
        try
        {
            Marshal.StructureToPtr(query, inBuffer, false);

            var outBuffer = Marshal.AllocHGlobal(maxLoadString);
            try
            {

                DeviceIoControl(
                    batteryHandle,
                    IOCTL_BATTERY_QUERY_INFORMATION,
                    inBuffer,
                    Marshal.SizeOf(query),
                    outBuffer,
                    maxLoadString);

                return Marshal.PtrToStringUni(outBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(inBuffer);
        }
    }

    private static BATTERY_STATUS GetBatteryStatus(SafeFileHandle batteryHandle, uint batteryTag)
    {
        BATTERY_WAIT_STATUS query = default;
        query.BatteryTag = batteryTag;

        var inBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(query));
        try
        {
            Marshal.StructureToPtr(query, inBuffer, false);

            var outBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(BATTERY_STATUS)));
            try
            {
                DeviceIoControl(
                    batteryHandle,
                    IOCTL_BATTERY_QUERY_STATUS,
                    inBuffer,
                    Marshal.SizeOf(query),
                    outBuffer,
                    Marshal.SizeOf(typeof(BATTERY_STATUS)));

                return Marshal.PtrToStructure<BATTERY_STATUS>(outBuffer);
            }
            finally
            {
                Marshal.FreeHGlobal(outBuffer);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(inBuffer);
        }
    }
}
