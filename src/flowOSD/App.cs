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

sealed partial class App : IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private IConfig config;

    private IMessageQueue messageQueue;
    private ISystemEvents systemEvents;
    private IPowerManagement powerManagement;
    private Display display;
    private IAtk atk;
    private ITouchPad touchPad;
    private IKeyboard keyboard;
    private IOsd osd;
    private IAudio audio;
    private IBattery battery;

    private INotifyIcon notifyIcon;

    private MainUI mainUI;

    private CommandManager commandManager;
    private HotKeyManager hotKeyManager;

    public App(IConfig config)
    {
        this.config = config;

        ApplicationContext = new ApplicationContext().DisposeWith(disposable);

        messageQueue = new MessageQueue().DisposeWith(disposable);

        keyboard = new Keyboard();
        powerManagement = new PowerManagement().DisposeWith(disposable);

        systemEvents = new SystemEvents(messageQueue).DisposeWith(disposable);

        var performanceMode = this.config.UserConfig.PerformanceModeOverrideEnabled
            ? this.config.UserConfig.PerformanceModeOverride
            : PerformanceMode.Default;

        atk = new Atk(performanceMode, messageQueue).DisposeWith(disposable);
        touchPad = new TouchPad(keyboard, messageQueue).DisposeWith(disposable);
        osd = new Osd(systemEvents).DisposeWith(disposable);

        display = new Display(messageQueue, powerManagement, config).DisposeWith(disposable);
        audio = new Audio();
        battery = new Battery().DisposeWith(disposable);

        systemEvents.AppException
            .Subscribe(ex =>
            {
                TraceException(ex, "Unhandled application exception");
                MessageBox.Show(ex.Message, "Unhandled application exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            })
            .DisposeWith(disposable);

        // Notifications

        InitNotifications();

        // Auto switching

        powerManagement.PowerEvent
            .Where(x => x == PowerEvent.Resume)
            .Throttle(TimeSpan.FromMicroseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ => Suspend())
            .DisposeWith(disposable);

        powerManagement.PowerEvent
            .Where(x => x == PowerEvent.Resume)
            .Throttle(TimeSpan.FromMicroseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ => Resume())
            .DisposeWith(disposable);

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
            new ToggleGpuCommand(atk),
            new PerformanceModeCommand(atk),
            new PowerModeCommand(powerManagement),
            new SettingsCommand(config, commandManager),
            new AboutCommand(config),
            new ExitCommand(),
            new PrintScreenCommand(keyboard),
            new ClipboardCopyPlainTextCommand(keyboard),
            new ClipboardPastePlainTextCommand(keyboard)
        );

        mainUI = new MainUI(config, systemEvents, commandManager, battery, powerManagement, atk);
        commandManager.Register(new MainUICommand(mainUI));

        InitNotifyIcon();     
        commandManager.Register(new NotifyIconMenuCommand(notifyIcon, commandManager, messageQueue, systemEvents));

        InitHotKeys();
    }

    private void Suspend()
    {

    }

    private void Resume()
    {
        if (config.UserConfig.PerformanceModeOverrideEnabled)
        {
            atk.SetPerformanceMode(config.UserConfig.PerformanceModeOverride);
        }
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