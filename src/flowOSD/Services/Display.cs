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
namespace flowOSD.Services;

using flowOSD.Api;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using static Native;

partial class Display : IDisposable, IDisplay
{
    public readonly static int WM_DISPLAY_CHANGE = (int)RegisterWindowMessage("UxdDisplayChangeMessage");

    private CompositeDisposable disposable = new CompositeDisposable();

    private RefreshRates refreshRates;
    private BehaviorSubject<bool> isHighRefreshRateSupportedSubject;
    private BehaviorSubject<bool> isHighRefreshRateSubject;

    public Display(IMessageQueue messageQueue)
    {
        refreshRates = GetSupportedRefreshRates();

        isHighRefreshRateSupportedSubject = new BehaviorSubject<bool>(refreshRates?.IsHighSupported == true);
        isHighRefreshRateSubject = new BehaviorSubject<bool>(refreshRates != null && GetRefreshRate() == refreshRates.High);

        IsHighRefreshRateSupported = isHighRefreshRateSupportedSubject.AsObservable();
        IsHighRefreshRate = isHighRefreshRateSubject.AsObservable();

        messageQueue.Subscribe(WM_DISPLAY_CHANGE, ProcessMessage).DisposeWith(disposable);
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public IObservable<bool> IsHighRefreshRateSupported { get; }

    public IObservable<bool> IsHighRefreshRate { get; }

    public void ToggleRefreshRate()
    {
        if (!isHighRefreshRateSubject.Value)
        {
            EnableHighRefreshRate();
        }
        else
        {
            DisableHighRefreshRate();
        }
    }

    public void EnableHighRefreshRate()
    {
        if (isHighRefreshRateSupportedSubject.Value && !isHighRefreshRateSubject.Value)
        {
            SetRefreshRate(refreshRates.High);
        }
    }

    public void DisableHighRefreshRate()
    {
        if (isHighRefreshRateSupportedSubject.Value && isHighRefreshRateSubject.Value)
        {
            SetRefreshRate(refreshRates.Default);
        }
    }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_DISPLAY_CHANGE)
        {
            refreshRates = GetSupportedRefreshRates();

            isHighRefreshRateSupportedSubject.OnNext(refreshRates?.IsHighSupported == true);
            isHighRefreshRateSubject.OnNext(refreshRates != null && GetRefreshRate() == refreshRates.High);
        }
    }

    private uint GetRefreshRate()
    {
        const int ENUM_CURRENT_SETTINGS = -1;

        var deviceName = GetLaptopDisplayAdapterDeviceName();
        if (string.IsNullOrEmpty(deviceName))
        {
            throw new ApplicationException("Can't find laptop display device.");
        }

        var mode = new DEVMODE();
        mode.dmSize = (ushort)Marshal.SizeOf(mode);

        if (!EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref mode))
        {
            throw new Win32Exception((int)GetLastError());
        }

        return mode.dmDisplayFrequency;
    }

    private void SetRefreshRate(uint refreshRate)
    {
        //Indicates that the function succeeded.
        const int DISP_CHANGE_SUCCESSFUL = 0;
        //The graphics mode is not supported.
        const int DISP_CHANGE_BADMODE = -2;
        //The computer must be restarted for the graphics mode to work.
        const int DISP_CHANGE_RESTART = 1;

        const int DM_DISPLAYFREQUENCY = 0x400000;
        const int CDS_UPDATEREGISTRY = 0x1;

        if (refreshRates == null)
        {
            throw new ApplicationException("Chaning refresh rate isn't supported.");
        }

        if (!refreshRates.Supports(refreshRate))
        {
            throw new ApplicationException($"Selected refresh rate ({refreshRate}) isn't supported.");
        }

        var deviceName = GetLaptopDisplayAdapterDeviceName();
        if (string.IsNullOrEmpty(deviceName))
        {
            throw new ApplicationException("Can't find laptop display device.");
        }

        var mode = new DEVMODE();
        mode.dmSize = (ushort)Marshal.SizeOf(mode);
        mode.dmDisplayFrequency = refreshRate;
        mode.dmFields = DM_DISPLAYFREQUENCY;

        var result = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
        switch (result)
        {
            case DISP_CHANGE_SUCCESSFUL:
                return;

            case DISP_CHANGE_RESTART:
                throw new ApplicationException($"Restart is required.");

            case DISP_CHANGE_BADMODE:
                throw new ApplicationException($"Selected refresh rate ({refreshRate}) isn't supported.");

            default:
                throw new ApplicationException($"Can't change display refresh rate. Error code: {result}.");
        }
    }

    private RefreshRates GetSupportedRefreshRates()
    {
        var rates = new HashSet<uint>();

        var deviceName = GetLaptopDisplayAdapterDeviceName();
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

        return rates.Count == 0 ? null : new RefreshRates(rates);
    }

    private string GetLaptopDisplayAdapterDeviceName()
    {
        const int DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x1;

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
                if (isAttached && displayMonitor.DeviceID?.Contains("SHP151E") == true)
                {
                    return displayAdapter.DeviceName;
                }

                displayMonitorNumber++;
            }

            displayAdapterNumber++;
        }

        return null;
    }

    private sealed class RefreshRates
    {
        public RefreshRates(ICollection<uint> values)
        {
            Default = values.Min();
            High = values.Max();

            IsHighSupported = High > 100 && values.Count > 1;
        }

        public uint Default { get; }

        public uint High { get; }

        public bool IsHighSupported { get; }

        public bool Supports(uint value) => value == Default || value == High;
    }
}