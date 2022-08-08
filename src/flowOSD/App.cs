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

    private ToolStripMenuItem highRefreshRateMenuItem, touchPadMenuItem, boostMenuItem, aboutMenuItem;
    private NotifyIcon notifyIcon;
    private NativeUI nativeUI;

    private CommandManager commandManager;

    public App(IConfig config)
    {
        this.config = config;

        ApplicationContext = new ApplicationContext().DisposeWith(disposable);

        Init();

        messageQueue = new MessageQueue().DisposeWith(disposable);
        imageSource = new ImageSource().DisposeWith(disposable);
        keyboard = new Keyboard();
        powerManagement = new PowerManagement().DisposeWith(disposable);

        systemEvents = new SystemEvents(messageQueue).DisposeWith(disposable);
        atk = new Atk(messageQueue).DisposeWith(disposable);
        touchPad = new TouchPad(keyboard, messageQueue).DisposeWith(disposable);
        osd = new Osd(systemEvents, imageSource).DisposeWith(disposable);

        display = new Display(messageQueue, powerManagement, config).DisposeWith(disposable);

        nativeUI = new NativeUI(notifyIcon.ContextMenuStrip.Handle, messageQueue).DisposeWith(disposable);

        systemEvents.TabletMode
            .CombineLatest(systemEvents.SystemDarkMode, nativeUI.Dpi.Throttle(TimeSpan.FromSeconds(2)), (isTabletMode, isDarkMode, dpi) => new { isTabletMode, isDarkMode, dpi })
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateNotifyIcon(x.isTabletMode, x.isDarkMode, x.dpi))
            .DisposeWith(disposable);

        systemEvents.AppException
            .Subscribe(ex =>
            {
                TraceException(ex, "Unhandled application exception");
                MessageBox.Show(ex.Message, "Unhandled application exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            })
            .DisposeWith(disposable);

        nativeUI.Dpi
            .Subscribe(dpi => UpdateDpi(dpi))
            .DisposeWith(disposable);

        // Hotkeys

        atk.KeyPressed
            .Where(x => x == AtkKey.BacklightDown || x == AtkKey.BacklightUp)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => osd.Show(new OsdData(Images.Keyboard, keyboard.GetBacklight())))
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

        // Auto switching

        systemEvents.TabletMode
            .Throttle(TimeSpan.FromSeconds(2))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => ToggleTouchPadOnTabletMode(x))
            .DisposeWith(disposable);

        // Commands

        commandManager = new CommandManager(
            new ToggleRefreshRateCommand(powerManagement, display, config.UserConfig),
            new ToggleTouchPadCommand(touchPad),
            new ToggleBoostCommand(powerManagement),
            new SettingsCommand(config),
            new AboutCommand(config),
            new ExitCommand(),
            new PrintScreenCommand(keyboard));

        BindCommandManager();

        // Hotkeys

        commandManager.Register(AtkKey.Aura, nameof(ToggleRefreshRateCommand));
        commandManager.Register(AtkKey.Fan, nameof(ToggleBoostCommand));
        commandManager.Register(AtkKey.Rog, nameof(PrintScreenCommand));

        atk.KeyPressed
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => commandManager.Resolve(x)?.Execute())
            .DisposeWith(disposable);
    }

    private void BindCommandManager()
    {
        foreach (var item in notifyIcon.ContextMenuStrip.Items)
        {
            if (item is CommandMenuItem commandItem)
            {
                commandItem.CommandManager = commandManager;
            }
        }
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

        osd.Show(new OsdData(Images.RefreshRate, isEnabled ? "High Refresh Rate is on" : "High Refresh Rate is off"));
    }

    private void ShowBoostNotification(bool isEnabled)
    {
        if (!config.UserConfig.ShowBoostNotification)
        {
            return;
        }

        osd.Show(new OsdData(Images.Boost, isEnabled ? "Boost Mode is on" : "Boost Mode is off"));
    }

    private void ShowTouchPadNotification(bool isEnabled)
    {
        if (!config.UserConfig.ShowTouchPadNotification)
        {
            return;
        }

        osd.Show(new OsdData(Images.TouchPad, isEnabled ? "TouchPad is on" : "TouchPad is off"));
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public ApplicationContext ApplicationContext { get; }

    private void Init()
    {
        notifyIcon = Create<NotifyIcon>().DisposeWith(disposable);
        notifyIcon.Click += (sender, e) =>
        {
            var methodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo.Invoke(notifyIcon, null);
        };

        notifyIcon.Text = $"{config.AppFileInfo.ProductName} ({config.AppFileInfo.ProductVersion})";
        notifyIcon.ContextMenuStrip = InitContextMenu();

        notifyIcon.Visible = true;
    }

    private ContextMenuStrip InitContextMenu()
    {
        return Create<ContextMenuStrip>(x =>
        {
            x.RenderMode = ToolStripRenderMode.System;
        }).Add(
            CreateCommandMenuItem(nameof(ToggleRefreshRateCommand)).DisposeWith(disposable).LinkAs(ref highRefreshRateMenuItem),
            CreateSeparator(highRefreshRateMenuItem).DisposeWith(disposable),
            CreateCommandMenuItem(nameof(ToggleTouchPadCommand)).DisposeWith(disposable).LinkAs(ref touchPadMenuItem),
            CreateCommandMenuItem(nameof(ToggleBoostCommand)).DisposeWith(disposable).LinkAs(ref boostMenuItem),
            CreateSeparator().DisposeWith(disposable),
            CreateCommandMenuItem(nameof(SettingsCommand)).DisposeWith(disposable).LinkAs(ref aboutMenuItem),
            CreateCommandMenuItem(nameof(AboutCommand)).DisposeWith(disposable).LinkAs(ref aboutMenuItem),
            CreateSeparator().DisposeWith(disposable),
            CreateCommandMenuItem(nameof(ExitCommand)).DisposeWith(disposable)
            );
    }

    private CommandMenuItem CreateCommandMenuItem(string commandName, object commandParameter = null)
    {
        var item = new CommandMenuItem();
        item.Padding = new Padding(0, 2, 0, 2);
        item.Margin = new Padding(0, 8, 0, 8);
        item.CommandName = commandName;
        item.CommandParameter = commandParameter;

        return item;
    }

    private ToolStripSeparator CreateSeparator(ToolStripItem dependsOn = null)
    {
        var separator = new ToolStripSeparator();

        if (dependsOn != null)
        {
            dependsOn.VisibleChanged += (sender, e) => separator.Visible = dependsOn.Visible;
        }

        return separator;
    }

    private void UpdateNotifyIcon(bool isTabletMode, bool isDarkMode, int dpi)
    {
        notifyIcon.Icon = null;

        if (isDarkMode)
        {
            notifyIcon.Icon = isTabletMode
                ? imageSource.GetIcon(Images.TabletWhite, dpi)
                : imageSource.GetIcon(Images.NotebookWhite, dpi);
        }
        else
        {
            notifyIcon.Icon = isTabletMode
                ? imageSource.GetIcon(Images.Tablet, dpi)
                : imageSource.GetIcon(Images.Notebook, dpi);
        }
    }

    private void UpdateDpi(int dpi)
    {
        notifyIcon.ContextMenuStrip.Font?.Dispose();
        notifyIcon.ContextMenuStrip.Font = new Font("Segoe UI", 12 * (dpi / 96f), GraphicsUnit.Pixel);
    }

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