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
using static Native;
using static Extensions;
using System.Management;

sealed partial class Display : IDisposable, IDisplay
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private IPowerManagement powerManagement;
    private IConfig config;

    private BehaviorSubject<bool> isEnabledSubject;
    private BehaviorSubject<DisplayRefreshRates> refreshRatesSubject;
    private BehaviorSubject<uint> refreshRateSubject;

    public Display(IMessageQueue messageQueue, IPowerManagement powerManagement, IConfig config)
    {
        this.powerManagement = powerManagement;
        this.config = config;

        var shortDeviceName = GetInternalDisplayShortDeviceName();
        if (shortDeviceName == null)
        {
            isEnabledSubject = new BehaviorSubject<bool>(false);
            refreshRatesSubject = new BehaviorSubject<DisplayRefreshRates>(DisplayRefreshRates.Empty);
            refreshRateSubject = new BehaviorSubject<uint>(0);
        }
        else
        {
            refreshRatesSubject = new BehaviorSubject<DisplayRefreshRates>(GetRefreshRates(shortDeviceName));
            isEnabledSubject = new BehaviorSubject<bool>(!refreshRatesSubject.Value.IsEmpty);
            refreshRateSubject = new BehaviorSubject<uint>(GetRefreshRate(shortDeviceName));
        }

        IsEnabled = isEnabledSubject.AsObservable();
        RefreshRates = refreshRatesSubject.AsObservable();
        RefreshRate = refreshRateSubject.AsObservable();

        messageQueue.Subscribe(WM_DISPLAYCHANGE, ProcessMessage).DisposeWith(disposable);

        this.powerManagement.IsDC
            .Throttle(TimeSpan.FromSeconds(2))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(OnPowerSourceChanged)
            .DisposeWith(disposable);
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public IObservable<bool> IsEnabled { get; }

    public IObservable<DisplayRefreshRates> RefreshRates { get; }

    public IObservable<uint> RefreshRate { get; }

    public bool SetRefreshRate(uint value)
    {
        var shortDeviceName = GetInternalDisplayShortDeviceName();
        if (shortDeviceName == null)
        {
            return false;
        }
        else
        {
            SetRefreshRate(shortDeviceName, value);
            return true;
        }
    }

    private void OnPowerSourceChanged(bool isDC)
    {
        if (!config.UserConfig.ControlDisplayRefreshRate)
        {
            return;
        }

        try
        {
            var isHighRefreshRate = isDC
                ? config.UserConfig.HighDisplayRefreshRateDC
                : config.UserConfig.HighDisplayRefreshRateAC;

            var refreshRate = isHighRefreshRate ? refreshRatesSubject.Value.High : refreshRatesSubject.Value.Low;
            if (refreshRate.HasValue)
            {
                SetRefreshRate(refreshRate.Value);
            }
        }
        catch (Exception ex)
        {
            TraceException(ex, "Error is occurred while toggling display refresh rate (Auto).");
        }
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
        var shortDeviceName = GetInternalDisplayShortDeviceName();
        if (shortDeviceName == null)
        {
            isEnabledSubject.OnNext(false);
            refreshRatesSubject.OnNext(DisplayRefreshRates.Empty);
            refreshRateSubject.OnNext(0);
        }
        else
        {
            refreshRatesSubject.OnNext(GetRefreshRates(shortDeviceName));
            isEnabledSubject.OnNext(!refreshRatesSubject.Value.IsEmpty);
            refreshRateSubject.OnNext(GetRefreshRate(shortDeviceName));
        }
    }

    private uint GetRefreshRate(string shortDeviceName)
    {
        var deviceName = GetDeviceName(shortDeviceName);
        if (string.IsNullOrEmpty(deviceName))
        {
            return 0;
        }

        var mode = new DEVMODE();
        mode.dmSize = (ushort)Marshal.SizeOf(mode);

        if (!EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref mode))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return mode.dmDisplayFrequency;
    }

    private bool SetRefreshRate(string shortDeviceName, uint value)
    {
        var refreshRates = refreshRatesSubject.Value;

        if (refreshRates.IsEmpty)
        {
            return false;
        }

        if (refreshRates.High != value && refreshRates.Low != value)
        {
            throw new ApplicationException($"Selected refresh rate ({value}) isn't supported.");
        }

        var deviceName = GetDeviceName(shortDeviceName);
        if (string.IsNullOrEmpty(deviceName))
        {
            return false;
        }

        var mode = new DEVMODE();
        mode.dmSize = (ushort)Marshal.SizeOf(mode);
        mode.dmDisplayFrequency = value;
        mode.dmFields = DM_DISPLAYFREQUENCY;

        var result = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
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

    private DisplayRefreshRates GetRefreshRates(string shortDeviceName)
    {
        var rates = new HashSet<uint>();

        var deviceName = GetDeviceName(shortDeviceName);
        if (!string.IsNullOrEmpty(deviceName))
        {
            var mode = new DEVMODE();
            mode.dmSize = (ushort)Marshal.SizeOf(mode);

            var modeNumber = 0;
            while (EnumDisplaySettings(deviceName, modeNumber, ref mode))
            {
                rates.Add(mode.dmDisplayFrequency);
                modeNumber++;
            }
        }

        return new DisplayRefreshRates(rates);
    }

    private string GetDeviceName(string shortDeviceName)
    {
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
                var isAttached = (displayMonitor.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) == DISPLAY_DEVICE_ATTACHED_TO_DESKTOP;
                var isMirroring = (displayMonitor.StateFlags & DISPLAY_DEVICE_MIRRORING_DRIVER) == DISPLAY_DEVICE_MIRRORING_DRIVER;

                if (isAttached && !isMirroring && displayMonitor.DeviceID?.Contains(shortDeviceName) == true)
                {
                    return displayAdapter.DeviceName;
                }

                displayMonitorNumber++;
            }

            displayAdapterNumber++;
        }

        return null;
    }

    private string GetInternalDisplayShortDeviceName()
    {
        string[] name = null;

        var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorConnectionParams");
        foreach (var i in searcher.Get())
        {
            if (i.Properties["VideoOutputTechnology"].Value is uint videoOutputTechnology
                && (videoOutputTechnology & D3DKMDT_VOT_INTERNAL) == D3DKMDT_VOT_INTERNAL)
            {
                name = ((i.Properties["InstanceName"].Value as string) ?? string.Empty).Split('\\');
                break;
            }
        }

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