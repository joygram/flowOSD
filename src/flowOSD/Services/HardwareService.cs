﻿/*  Copyright © 2021-2023, Albert Akhmetov <akhmetov@live.com>   
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

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using flowOSD.Api;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;
using flowOSD.Hardware;
using flowOSD.Native;
using Microsoft.Win32;

sealed class HardwareService : IDisposable, IHardwareService
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private IMessageQueue messageQueue;

    private HidDevice hidDevice;

    private Atk atk;
    private AtkWmi atkWmi;
    private Cpu cpu;
    private Keyboard keyboard;
    private KeyboardBacklight keyboardBacklight;
    private TouchPad touchPad;
    private Display display;
    private Battery battery;
    private PowerManagement powerManagement;
    private Microphone microphone;

    private Dictionary<Type, object> devices = new Dictionary<Type, object>();

    private KeyboardBacklightService keyboardBacklightService;

    public HardwareService(IConfig config, IMessageQueue messageQueue)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));

        hidDevice = HidDevice.Devices
            .Where(i => i.VendorId == 0xB05 && i.ReadFeatureData(out byte[] data, Keyboard.FEATURE_KBD_REPORT_ID))
            .FirstOrDefault() ?? throw new ApplicationException("Can't connect to HID");

        InitHid();

        atk = new Atk(config.UserConfig.PerformanceModeOverrideEnabled ? config.UserConfig.PerformanceModeOverride : null);
        atkWmi = new AtkWmi(atk);
        cpu = new Cpu();

        keyboard = new Keyboard(hidDevice);
        keyboardBacklight = new KeyboardBacklight(hidDevice, KeyboardBacklightLevel.Low); // << change to config
        touchPad = new TouchPad(hidDevice);

        display = new Display(this.messageQueue);

        battery = new Battery();
        powerManagement = new PowerManagement();

        microphone = new Microphone();

        Register<IAtk>(atk);
        Register<IAtkWmi>(atkWmi);
        Register<ICpu>(cpu);
        Register<IKeyboard>(keyboard);
        Register<IKeyboardBacklight>(keyboardBacklight);
        Register<ITouchPad>(touchPad);
        Register<IDisplay>(display);
        Register<IBattery>(battery);
        Register<IPowerManagement>(powerManagement);
        Register<IMicrophone>(microphone);

        powerManagement.PowerEvent
           .Where(x => x == PowerEvent.Resume)
           .Throttle(TimeSpan.FromMicroseconds(50))
           .ObserveOn(SynchronizationContext.Current!)
           .Subscribe(_ => OnResume())
           .DisposeWith(disposable);

        touchPad.State
            .CombineLatest(atkWmi.TabletMode, (touchPadState, tabletMode) => new { touchPadState, tabletMode })
            .Throttle(TimeSpan.FromMicroseconds(2000))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => UpdateTouchPad(x.touchPadState, x.tabletMode))
            .DisposeWith(disposable);

        keyboard.KeyPressed
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Where(x => x == AtkKey.BacklightDown)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => keyboardBacklight.LevelDown())
            .DisposeWith(disposable);

        keyboard.KeyPressed
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Where(x => x == AtkKey.BacklightUp)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(_ => keyboardBacklight.LevelUp())
            .DisposeWith(disposable);

        keyboard.KeyPressed
            .Where(x => x == AtkKey.BrightnessDown || x == AtkKey.BrightnewssUp)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => UpdateBrightness(x))
            .DisposeWith(disposable);

        keyboard.KeyPressed
            .Where(x => x == AtkKey.Mic)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => microphone.Toggle())
            .DisposeWith(disposable);

        keyboard.KeyPressed
            .Where(x => x == AtkKey.TouchPad)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => touchPad.Toggle())
            .DisposeWith(disposable);

        keyboard.KeyPressed
            .Where(x => x == AtkKey.Sleep)
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => Powrprof.SetSuspendState(true, true, true))
            .DisposeWith(disposable);

        keyboardBacklightService = new KeyboardBacklightService(
            keyboardBacklight, 
            keyboard, 
            TimeSpan.FromSeconds(60)).DisposeWith(disposable);
    }

    public void Dispose()
    {
        if (disposable != null)
        {
            disposable.Dispose();
            disposable = null;

            foreach (var i in devices.Values)
            {
                (i as IDisposable)?.Dispose();
            }
        }
    }

    public T? Resolve<T>() where T : class
    {
        var isOk = devices.TryGetValue(typeof(T), out object? value);

        return isOk && value is T device ? device : null;
    }

    public T ResolveNotNull<T>() where T : class
    {
        return Resolve<T>() ?? throw new InvalidOperationException($"Can't resolve {typeof(T).Name}");
    }

    private void Register<T>(T instance) where T : class
    {
        if (instance == null)
        {
            devices.Remove(typeof(T));
        }
        else
        {
            devices[typeof(T)] = instance;
        }
    }

    private void OnResume()
    {
        if (config.UserConfig.PerformanceModeOverrideEnabled)
        {
            atk.SetPerformanceMode(config.UserConfig.PerformanceModeOverride);
        }

        InitHid();
        keyboardBacklightService.ResetTimer();
        keyboardBacklight.SetState(DeviceState.Enabled, force: true);
    }

    private void InitHid()
    {
#if !DEBUG
        hidDevice.WriteFeatureData(0x5a, 0x89);
        hidDevice.WriteFeatureData(0x5a, 0x41, 0x53, 0x55, 0x53, 0x20, 0x54, 0x65, 0x63, 0x68, 0x2e, 0x49, 0x6e, 0x63, 0x2e);
        hidDevice.WriteFeatureData(0x5a, 0x05, 0x20, 0x31, 0x00, 0x08);
#endif
    }

    private void UpdateTouchPad(DeviceState touchPadState, TabletMode tabletMode)
    {
        if (!config.UserConfig.DisableTouchPadInTabletMode)
        {
            return;
        }

        if (tabletMode == TabletMode.Notebook && touchPadState == DeviceState.Disabled)
        {
            touchPad.Toggle();
            return;
        }

        if (tabletMode != TabletMode.Notebook && touchPadState != DeviceState.Disabled)
        {
            touchPad.Toggle();
            return;
        }
    }

    private void UpdateBrightness(AtkKey key)
    {
        var delta = key == AtkKey.BrightnessDown ? -0.1 : +0.1;
        var brightness = display.GetBrightness();

        display.SetBrightness((brightness + delta));
    }
}
