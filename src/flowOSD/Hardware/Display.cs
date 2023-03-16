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
namespace flowOSD.Hardware;

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Api;
using static Native.User32;
using static Native.Messages;
using System.Management;
using flowOSD.Native;
using flowOSD.Extensions;
using flowOSD.Api.Hardware;

sealed partial class Display : IDisposable, IDisplay
{
    private const uint D3DKMDT_VOT_INTERNAL = 0x80000000;

    private CompositeDisposable? disposable = new CompositeDisposable();

    private BehaviorSubject<DeviceState> isStateSubject;
    private BehaviorSubject<DisplayRefreshRates> refreshRatesSubject;
    private BehaviorSubject<uint> refreshRateSubject;
    private Subject<double> brightnessSubject;

    public Display(IMessageQueue messageQueue)
    {
        refreshRatesSubject = new BehaviorSubject<DisplayRefreshRates>(GetRefreshRates());
        isStateSubject = new BehaviorSubject<DeviceState>(GetDeviceState());
        refreshRateSubject = new BehaviorSubject<uint>(GetRefreshRate());
        brightnessSubject = new Subject<double>();

        State = isStateSubject.AsObservable();
        RefreshRates = refreshRatesSubject.AsObservable();
        RefreshRate = refreshRateSubject.AsObservable();
        Brightness = brightnessSubject.AsObservable();

        messageQueue.Subscribe(WM_DISPLAYCHANGE, ProcessMessage).DisposeWith(disposable);
    }

    public IObservable<DeviceState> State { get; }

    public IObservable<DisplayRefreshRates> RefreshRates { get; }

    public IObservable<uint> RefreshRate { get; }

    public IObservable<double> Brightness { get; }

    public bool SetRefreshRate(uint value)
    {
        if (!GetDeviceName(out var deviceName))
        {
            return false;
        }

        var refreshRates = refreshRatesSubject.Value;

        if (refreshRates.IsEmpty)
        {
            return false;
        }

        if (refreshRates.High != value && refreshRates.Low != value)
        {
            throw new ApplicationException($"Selected refresh rate ({value}) isn't supported.");
        }

        var mode = new DEVMODE();
        mode.dmSize = (ushort)Marshal.SizeOf(mode);
        mode.dmDisplayFrequency = value;
        mode.dmFields = DM_DISPLAYFREQUENCY;

        var result = ChangeDisplaySettingsEx(deviceName!, ref mode, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
        switch (result)
        {
            case DISP_CHANGE_SUCCESSFUL:
                return true;

            case DISP_CHANGE_RESTART:
                throw new ApplicationException($"Restart is required.");

            case DISP_CHANGE_BADMODE:
                throw new ApplicationException($"Selected refresh rate ({value}) isn't supported.");

            default:
                throw new ApplicationException($"Can't change display refresh rate. Error code: {result}.");
        }
    }

    public double GetBrightness()
    {
        if (GetBrightness(GetInternalDisplayDeviceName(), out _, out _, out var value))
        {
            return value!.Value;
        }

        return 0;
    }

    public void SetBrightness(double value)
    {
        var deviceName = GetInternalDisplayDeviceName();
        if (string.IsNullOrEmpty(deviceName) || !GetBrightness(deviceName, out var level, out var levels, out var oldValue))
        {
            return;
        }

        var newValue = Math.Max(0, Math.Min(1, Math.Round(value * 10) / 10));
        var newIndex = (int)Math.Round((levels!.Length - 1) * newValue);

        using var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorBrightnessMethods");
        foreach (ManagementObject i in searcher.Get())
        {
            if (i.Properties["InstanceName"].Value as string != deviceName)
            {
                continue;
            }

            i.InvokeMethod("WmiSetBrightness", new object[] { uint.MaxValue, levels[newIndex] });
            var sign = Math.Sign(newValue - oldValue!.Value);
            brightnessSubject.OnNext((sign == 0 ? 1 : sign) * newValue);

            return;
        }
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private DeviceState GetDeviceState()
    {
        return refreshRatesSubject.Value.IsEmpty ? DeviceState.Disabled : DeviceState.Enabled;
    }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_DISPLAYCHANGE)
        {
            UpdateRefreshRates();
        }
    }

    private void UpdateRefreshRates()
    {
        refreshRatesSubject.OnNext(GetRefreshRates());
        isStateSubject.OnNext(GetDeviceState());
        refreshRateSubject.OnNext(GetRefreshRate());
    }

    private uint GetRefreshRate()
    {
        if (!GetDeviceName(out var deviceName))
        {
            return 0;
        }

        var mode = new DEVMODE();
        mode.dmSize = (ushort)Marshal.SizeOf(mode);

        if (!EnumDisplaySettings(deviceName!, ENUM_CURRENT_SETTINGS, ref mode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return mode.dmDisplayFrequency;
    }

    private DisplayRefreshRates GetRefreshRates()
    {
        var rates = new HashSet<uint>();

        if (GetDeviceName(out var deviceName))
        {
            var mode = new DEVMODE();
            mode.dmSize = (ushort)Marshal.SizeOf(mode);

            var modeNumber = 0;
            while (EnumDisplaySettings(deviceName!, modeNumber, ref mode))
            {
                rates.Add(mode.dmDisplayFrequency);
                modeNumber++;
            }
        }

        return new DisplayRefreshRates(rates);
    }

    private bool GetBrightness(string? deviceName, out byte? level, out byte[]? levels, out double? value)
    {
        if (!string.IsNullOrEmpty(deviceName))
        {
            var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorBrightness");
            foreach (var i in searcher.Get())
            {
                if (i.Properties["InstanceName"].Value as string != deviceName)
                {
                    continue;
                }

                if (i.Properties["CurrentBrightness"].Value is byte && i.Properties["Level"].Value is byte[])
                {
                    level = (byte)i.Properties["CurrentBrightness"].Value;
                    levels = (byte[])i.Properties["Level"].Value;

                    value = 1d * Array.IndexOf(levels!, level) / (levels!.Length - 1);

                    return true;
                }

            }
        }

        level = null;
        levels = null;
        value = null;

        return false;
    }

    private bool GetDeviceName(out string? deviceName)
    {
        deviceName = null;

        var shortDeviceName = GetInternalDisplayShortDeviceName();
        if (shortDeviceName == null)
        {
            return false;
        }

        var displayAdapter = new DISPLAY_DEVICE();
        displayAdapter.cb = Marshal.SizeOf<DISPLAY_DEVICE>();

        var displayAdapterNumber = default(uint);
        while (EnumDisplayDevices(null, displayAdapterNumber, ref displayAdapter, 1))
        {
            var displayMonitor = new DISPLAY_DEVICE();
            displayMonitor.cb = Marshal.SizeOf<DISPLAY_DEVICE>();

            var displayMonitorNumber = default(uint);
            while (EnumDisplayDevices(displayAdapter.DeviceName, displayMonitorNumber, ref displayMonitor, 1))
            {
                var isAttached = (displayMonitor.StateFlags & DisplayDeviceStates.ATTACHED_TO_DESKTOP) == DisplayDeviceStates.ATTACHED_TO_DESKTOP;
                var isMirroring = (displayMonitor.StateFlags & DisplayDeviceStates.MIRRORING_DRIVER) == DisplayDeviceStates.MIRRORING_DRIVER;

                if (isAttached && !isMirroring && displayMonitor.DeviceID?.Contains(shortDeviceName) == true)
                {
                    deviceName = displayAdapter.DeviceName;

                    return true;
                }

                displayMonitorNumber++;
            }

            displayAdapterNumber++;
        }

        return false;
    }

    private string? GetInternalDisplayDeviceName()
    {
        var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorConnectionParams");
        foreach (var i in searcher.Get())
        {
            if (i.Properties["VideoOutputTechnology"].Value is uint videoOutputTechnology
                && (videoOutputTechnology & D3DKMDT_VOT_INTERNAL) == D3DKMDT_VOT_INTERNAL)
            {
                return i.Properties["InstanceName"].Value as string;
            }
        }

        return null;
    }

    private string? GetInternalDisplayShortDeviceName()
    {
        var name = (GetInternalDisplayDeviceName() ?? string.Empty).Split('\\');

        if (name != null && name.Length > 1 && name[0] == "DISPLAY")
        {
            return $"{name[0]}#{name[1]}";
        }
        else
        {
            return null;
        }
    }

}