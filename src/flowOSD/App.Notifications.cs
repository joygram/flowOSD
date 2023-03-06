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

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using flowOSD.Api;
using flowOSD.Services;
using flowOSD.UI;
using flowOSD.UI.Commands;
using flowOSD.UI.Components;
using static Extensions;

partial class App
{
    private void InitNotifications()
    {
        atk.PerformanceMode
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(ShowPerformanceModeNotification)
            .DisposeWith(disposable);

        powerManagement.PowerMode
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(ShowPowerModeNotification)
            .DisposeWith(disposable);

        powerManagement.IsDC
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromSeconds(2))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(ShowPowerSourceNotification)
            .DisposeWith(disposable);

        touchPad.IsEnabled
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(ShowTouchPadNotification)
            .DisposeWith(disposable);

        powerManagement.IsBoost
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(ShowBoostNotification)
            .DisposeWith(disposable);

        display.IsHighRefreshRate
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(ShowDisplayRefreshRateNotification)
            .DisposeWith(disposable);

        atk.GpuMode
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(ShowGpuNotification)
            .DisposeWith(disposable);

        // Keyboard Backlight

        atk.KeyPressed
            .Where(x => x == AtkKey.BacklightDown || x == AtkKey.BacklightUp)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => ShowKeyboardBacklightNotification(x))
            .DisposeWith(disposable);

        // Mic Status

        atk.KeyPressed
            .Where(x => x == AtkKey.MuteMic)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => ShowMicNotification())
            .DisposeWith(disposable);
    }

    private void ShowKeyboardBacklightNotification(AtkKey x)
    {
        var icon = x == AtkKey.BacklightDown
            ? UIImages.Hardware_KeyboardLightDown
            : UIImages.Hardware_KeyboardLightUp;

        osd.Show(new OsdData(icon, keyboard.GetBacklight()));
    }

    private void ShowMicNotification()
    {
        if (!config.UserConfig.ShowMicNotification)
        {
            return;
        }

        try
        {
            var isMuted = audio.IsMicMuted();
            osd.Show(new OsdData(
                isMuted ? UIImages.Hardware_MicMuted : UIImages.Hardware_Mic,
                isMuted ? "Muted" : "On air"));
        }
        catch (Exception ex)
        {
            TraceException(ex, "Error is occurred while toggling TouchPad state (Auto).");
        }
    }

    private void ShowPerformanceModeNotification(PerformanceMode performanceMode)
    {
        if (!config.UserConfig.ShowPerformanceModeNotification)
        {
            return;
        }

        switch (performanceMode)
        {
            case PerformanceMode.Default:
                {
                    osd.Show(new OsdData(UIImages.Performance_Default, $"{performanceMode.ToText()} performance mode"));
                    break;
                }

            case PerformanceMode.Turbo:
                {
                    osd.Show(new OsdData(UIImages.Performance_Turbo, $"{performanceMode.ToText()} performance mode"));
                    break;
                }

            case PerformanceMode.Silent:
                {
                    osd.Show(new OsdData(UIImages.Performance_Silent, $"{performanceMode.ToText()} performance mode"));
                    break;
                }
        }
    }

    private void ShowPowerModeNotification(PowerMode powerMode)
    {
        if (!config.UserConfig.ShowPowerModeNotification)
        {
            return;
        }

        switch (powerMode)
        {
            case PowerMode.BestPowerEfficiency:
                {
                    osd.Show(new OsdData(UIImages.Power_BestPowerEfficiency, $"{powerMode.ToText()} power mode"));
                    break;
                }

            case PowerMode.Balanced:
                {
                    osd.Show(new OsdData(UIImages.Power_Balanced, $"{powerMode.ToText()} power mode"));
                    break;
                }

            case PowerMode.BestPerformance:
                {
                    osd.Show(new OsdData(UIImages.Power_BestPerformance, $"{powerMode.ToText()} power mode"));
                    break;
                }
        }
    }

    private void ShowPowerSourceNotification(bool isBattery)
    {
        if (!config.UserConfig.ShowPowerSourceNotification)
        {
            return;
        }

        osd.Show(new OsdData(isBattery ? UIImages.Hardware_DC : UIImages.Hardware_AC, isBattery ? "On Battery" : "Plugged In"));
    }

    private void ShowDisplayRefreshRateNotification(bool isEnabled)
    {
        if (!config.UserConfig.ShowDisplayRateNotification)
        {
            return;
        }

        osd.Show(new OsdData(UIImages.Hardware_Screen, isEnabled ? "High Refresh Rate" : "Low Refresh Rate"));
    }

    private void ShowBoostNotification(bool isEnabled)
    {
        if (!config.UserConfig.ShowBoostNotification)
        {
            return;
        }

        osd.Show(new OsdData(UIImages.Hardware_Cpu, isEnabled ? "Boost Mode is on" : "Boost Mode is off"));
    }

    private void ShowTouchPadNotification(bool isEnabled)
    {
        if (!config.UserConfig.ShowTouchPadNotification)
        {
            return;
        }

        osd.Show(new OsdData(UIImages.Hardware_TouchPad, isEnabled ? "TouchPad is on" : "TouchPad is off"));
    }

    private void ShowGpuNotification(GpuMode gpuMode)
    {
        if (!config.UserConfig.ShowGpuNotification)
        {
            return;
        }

        osd.Show(new OsdData(UIImages.Hardware_Gpu, gpuMode == GpuMode.dGpu ? "eGPU is on" : "eGPU is off"));
    }

}
