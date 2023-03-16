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
using flowOSD.Api.Hardware;
using flowOSD.Extensions;
using flowOSD.Services;
using flowOSD.UI;
using flowOSD.UI.Commands;
using flowOSD.UI.Components;
using static flowOSD.Extensions.Common;

sealed class Notifications : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private IOsd osd;

    private IAtk atk;
    private IPowerManagement powerManagement;
    private ITouchPad touchPad;
    private IDisplay display;
    private IKeyboard keyboard;
    private IKeyboardBacklight keyboardBacklight;
    private IMicrophone microphone;

    public Notifications(IConfig config, IOsd osd, IHardwareService hardwareManager)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.osd = osd ?? throw new ArgumentNullException(nameof(osd));

        if (hardwareManager == null)
        {
            throw new ArgumentNullException("hardwareManager");
        }

        atk = hardwareManager.ResolveNotNull<IAtk>();
        powerManagement = hardwareManager.ResolveNotNull<IPowerManagement>();
        touchPad = hardwareManager.ResolveNotNull<ITouchPad>();
        display = hardwareManager.ResolveNotNull<IDisplay>();
        keyboard = hardwareManager.ResolveNotNull<IKeyboard>();
        keyboardBacklight = hardwareManager.ResolveNotNull<IKeyboardBacklight>();
        microphone = hardwareManager.ResolveNotNull<IMicrophone>();

        Init(disposable);
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void Init(CompositeDisposable disposable)
    {
        atk.PerformanceMode
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowPerformanceModeNotification)
            .DisposeWith(disposable);

        powerManagement.PowerMode
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowPowerModeNotification)
            .DisposeWith(disposable);

        powerManagement.IsDC
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromSeconds(2))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowPowerSourceNotification)
            .DisposeWith(disposable);

        touchPad.State
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowTouchPadNotification)
            .DisposeWith(disposable);

        powerManagement.IsBoost
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowBoostNotification)
            .DisposeWith(disposable);

        display.RefreshRate
            .CombineLatest(display.State, (refreshRate, displayState) => new { refreshRate, displayState })
            .Where(x => x.displayState == DeviceState.Enabled)
            .Select(x => x.refreshRate)
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowDisplayRefreshRateNotification)
            .DisposeWith(disposable);

        atk.GpuMode
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowGpuNotification)
            .DisposeWith(disposable);

        // Keyboard Backlight

        keyboard.KeyPressed
            .Where(x => x == AtkKey.BacklightDown || x == AtkKey.BacklightUp)
            .CombineLatest(keyboardBacklight.Level, (key, level) => new { key, level })
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => ShowKeyboardBacklightNotification(x.key, x.level))
            .DisposeWith(disposable);

        keyboard.KeyPressed
            .Where(x => x == AtkKey.BrightnessDown || x == AtkKey.BrightnewssUp)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ShowDisplayBrightnessNotification)
            .DisposeWith(disposable);

        // Mic Status

        keyboard.KeyPressed
            .Where(x => x == AtkKey.Mic)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => ShowMicNotification())
            .DisposeWith(disposable);
    }

    private void ShowKeyboardBacklightNotification(AtkKey key, KeyboardBacklightLevel backlightLevel)
    {
        var icon = key == AtkKey.BacklightDown
            ? UIImages.Hardware_KeyboardLightDown
            : UIImages.Hardware_KeyboardLightUp;

        osd.Show(new OsdData(icon, (float)backlightLevel / (float)KeyboardBacklightLevel.High));
    }

    private void ShowDisplayBrightnessNotification(AtkKey key)
    {
        var icon = key == AtkKey.BacklightDown
            ? UIImages.Hardware_BrightnessDown
            : UIImages.Hardware_BrightnessUp;

        osd.Show(new OsdData(icon, display.GetBrightness()));
    }

    private void ShowMicNotification()
    {
        if (!config.UserConfig.ShowMicNotification)
        {
            return;
        }

        try
        {
            var isMuted = microphone.IsMicMuted();
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

    private void ShowDisplayRefreshRateNotification(uint refreshRate)
    {
        if (!config.UserConfig.ShowDisplayRateNotification)
        {
            return;
        }

        osd.Show(new OsdData(UIImages.Hardware_Screen, DisplayRefreshRates.IsHigh(refreshRate) ? "High Refresh Rate" : "Low Refresh Rate"));
    }

    private void ShowBoostNotification(bool isEnabled)
    {
        if (!config.UserConfig.ShowBoostNotification)
        {
            return;
        }

        osd.Show(new OsdData(UIImages.Hardware_Cpu, isEnabled ? "Boost Mode is on" : "Boost Mode is off"));
    }

    private void ShowTouchPadNotification(DeviceState state)
    {
        if (!config.UserConfig.ShowTouchPadNotification)
        {
            return;
        }

        osd.Show(new OsdData(UIImages.Hardware_TouchPad, state == DeviceState.Enabled ? "TouchPad is on" : "TouchPad is off"));
    }

    private void ShowGpuNotification(GpuMode gpuMode)
    {
        if (!config.UserConfig.ShowGpuNotification)
        {
            return;
        }

        osd.Show(new OsdData(UIImages.Hardware_Gpu, gpuMode == GpuMode.dGpu ? "dGPU is on" : "dGPU is off"));
    }
}
