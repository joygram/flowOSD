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
using static Extensions;

sealed class App : IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private IConfig config;

    private IMessageQueue messageQueue;
    private ISystemEvents systemEvents;
    private IImageSource imageSource;
    private IPowerManagement powerManagement;
    private Display display;
    private IAtk atk;
    private ITouchPad touchPad;
    private IKeyboard keyboard;
    private IOsd osd;
    private IAudio audio;
    private IGpu gpu;
    private IBattery battery;

    private TrayIcon trayIcon;
    private MainUI mainUI;

    private CommandManager commandManager;
    private HotKeyManager hotKeyManager;

    public App(IConfig config)
    {
        this.config = config;

        ApplicationContext = new ApplicationContext().DisposeWith(disposable);


        messageQueue = new MessageQueue().DisposeWith(disposable);
        imageSource = new ImageSource().DisposeWith(disposable);

        keyboard = new Keyboard();
        powerManagement = new PowerManagement().DisposeWith(disposable);

        systemEvents = new SystemEvents(messageQueue).DisposeWith(disposable);
        atk = new Atk(messageQueue).DisposeWith(disposable);
        touchPad = new TouchPad(keyboard, messageQueue).DisposeWith(disposable);
        osd = new Osd(systemEvents).DisposeWith(disposable);

        display = new Display(messageQueue, powerManagement, config).DisposeWith(disposable);
        audio = new Audio();
        gpu = new Gpu(atk).DisposeWith(disposable);
        battery = new Battery().DisposeWith(disposable);

        mainUI = new MainUI(config, systemEvents, messageQueue);

        systemEvents.AppException
            .Subscribe(ex =>
            {
                TraceException(ex, "Unhandled application exception");
                MessageBox.Show(ex.Message, "Unhandled application exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            })
            .DisposeWith(disposable);

        // Notifications

        powerManagement.IsDC
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromSeconds(2))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => ShowPowerSourceNotification(x))
            .DisposeWith(disposable);

        touchPad.IsEnabled
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => ShowTouchPadNotification(x))
            .DisposeWith(disposable);

        powerManagement.IsBoost
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => ShowBoostNotification(x))
            .DisposeWith(disposable);

        display.IsHighRefreshRate
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => ShowDisplayRefreshRateNotification(x))
            .DisposeWith(disposable);

        gpu.IsEnabled
            .Skip(1)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => ShowGpuNotification(x))
            .DisposeWith(disposable);

        // Keyboard Backlight

        atk.KeyPressed
            .Where(x => x == AtkKey.BacklightDown || x == AtkKey.BacklightUp)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => osd.Show(new OsdData(
                x == AtkKey.BacklightDown ? Images.KeyboardLowerBrightness : Images.KeyboardBrightness,
                keyboard.GetBacklight())))
            .DisposeWith(disposable);

        // Mic Status

        atk.KeyPressed
            .Where(x => x == AtkKey.MuteMic)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x =>
            {
                if (!config.UserConfig.ShowMicNotification)
                {
                    return;
                }

                try
                {
                    var isMuted = audio.IsMicMuted();
                    osd.Show(new OsdData(
                        isMuted ? Images.MicMuted : Images.Mic,
                        isMuted ? "Muted" : "On air"));
                }
                catch (Exception ex)
                {
                    TraceException(ex, "Error is occurred while toggling TouchPad state (Auto).");
                }
            })
            .DisposeWith(disposable);

        // Auto switching

        systemEvents.TabletMode
            .Throttle(TimeSpan.FromSeconds(2))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => ToggleTouchPadOnTabletMode(x))
            .DisposeWith(disposable);

        // Commands

        commandManager = new CommandManager();
        commandManager.Register(
            new ToggleRefreshRateCommand(powerManagement, display, config.UserConfig),
            new ToggleTouchPadCommand(touchPad),
            new ToggleBoostCommand(powerManagement),
            new ToggleGpuCommand(gpu),
            new SettingsCommand(config, commandManager),
            new AboutCommand(config),
            new ExitCommand(),
            new PrintScreenCommand(keyboard),
            new ClipboardCopyPlainTextCommand(keyboard),
            new ClipboardPastePlainTextCommand(keyboard)
        );

        trayIcon = new TrayIcon(
            mainUI,
            config,
            imageSource,
            commandManager,
            systemEvents,
            battery,
            messageQueue).DisposeWith(disposable);

        // Hotkeys

        hotKeyManager = new HotKeyManager(commandManager);
        config.UserConfig.PropertyChanged.Subscribe(propertyName =>
        {
            switch (propertyName)
            {
                case nameof(UserConfig.AuraCommand):
                    hotKeyManager.Register(AtkKey.Aura, config.UserConfig.AuraCommand);
                    break;

                case nameof(UserConfig.FanCommand):
                    hotKeyManager.Register(AtkKey.Fan, config.UserConfig.FanCommand);
                    break;

                case nameof(UserConfig.RogCommand):
                    hotKeyManager.Register(AtkKey.Rog, config.UserConfig.RogCommand);
                    break;

                case nameof(UserConfig.CopyCommand):
                    hotKeyManager.Register(AtkKey.Copy, config.UserConfig.CopyCommand);
                    break;

                case nameof(UserConfig.PasteCommand):
                    hotKeyManager.Register(AtkKey.Paste, config.UserConfig.PasteCommand);
                    break;

                case "":
                case null:
                    RegisterHotKeys();
                    break;
            }
        }).DisposeWith(disposable);

        RegisterHotKeys();

        atk.KeyPressed
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => hotKeyManager.ExecuteCommand(x))
            .DisposeWith(disposable);
    }


    private void RegisterHotKeys()
    {
        hotKeyManager.Register(AtkKey.Aura, config.UserConfig.AuraCommand);
        hotKeyManager.Register(AtkKey.Fan, config.UserConfig.FanCommand);
        hotKeyManager.Register(AtkKey.Rog, config.UserConfig.RogCommand);
        hotKeyManager.Register(AtkKey.Copy, config.UserConfig.CopyCommand);
        hotKeyManager.Register(AtkKey.Paste, config.UserConfig.PasteCommand);
    }

    private void ShowPowerSourceNotification(bool isBattery)
    {
        if (!config.UserConfig.ShowPowerSourceNotification)
        {
            return;
        }

        osd.Show(new OsdData(isBattery ? Images.DC : Images.AC, isBattery ? "On Battery" : "Plugged In"));
    }

    private void ShowDisplayRefreshRateNotification(bool isEnabled)
    {
        if (!config.UserConfig.ShowDisplayRateNotification)
        {
            return;
        }

        if (isEnabled)
        {
            osd.Show(new OsdData(Images.HiRefreshRate, "High Refresh Rate"));
        }
        else
        {
            osd.Show(new OsdData(Images.LowRefreshRate, "Low Refresh Rate"));
        }
    }

    private void ShowBoostNotification(bool isEnabled)
    {
        if (!config.UserConfig.ShowBoostNotification)
        {
            return;
        }

        if (isEnabled)
        {
            osd.Show(new OsdData(Images.BoostOn, "Boost Mode is on"));
        }
        else
        {
            osd.Show(new OsdData(Images.BoostOff, "Boost Mode is off"));
        }
    }

    private void ShowTouchPadNotification(bool isEnabled)
    {
        if (!config.UserConfig.ShowTouchPadNotification)
        {
            return;
        }

        osd.Show(new OsdData(Images.TouchPad, isEnabled ? "TouchPad is on" : "TouchPad is off"));
    }

    private void ShowGpuNotification(bool isEnabled)
    {
        if (!config.UserConfig.ShowGpuNotification)
        {
            return;
        }

        osd.Show(new OsdData(Images.Gpu, isEnabled ? "eGPU is on" : "eGPU is off"));
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public ApplicationContext ApplicationContext { get; }

    private void ToggleTouchPadOnTabletMode(bool isTabletMode)
    {
        if (!config.UserConfig.DisableTouchPadInTabletMode)
        {
            return;
        }

        try
        {
            if (isTabletMode)
            {
                touchPad.Disable();
            }
            else
            {
                touchPad.Enable();
            }
        }
        catch (Exception ex)
        {
            TraceException(ex, "Error is occurred while toggling TouchPad state (Auto).");
        }
    }
}