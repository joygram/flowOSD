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
namespace flowOSD.UI;

using System;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Input;
using flowOSD.Api;
using flowOSD.Services;
using flowOSD.UI.Commands;
using flowOSD.UI.Components;
using static Extensions;
using static Native;

sealed class TrayIcon : IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();
    private NotifyIcon notifyIcon;

    private NativeUI nativeUI;
    private ICommandManager commandManager;
    private IConfig config;
    private IImageSource imageSource;
    private ISystemEvents systemEvents;
    private IBattery battery;

    private ToolStripItem batteryMenuItem, monitoringSeparator;

    public TrayIcon(
        NativeUI nativeUI,
        IConfig config,
        IImageSource imageSource,
        ICommandManager commandManager,
        ISystemEvents systemEvents,
        IBattery battery)
    {
        this.nativeUI = nativeUI ?? throw new ArgumentNullException(nameof(nativeUI));

        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.imageSource = imageSource ?? throw new ArgumentNullException(nameof(imageSource));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        this.battery = battery ?? throw new ArgumentNullException(nameof(battery));

        Init();

        nativeUI.Dpi
            .CombineLatest(systemEvents.SystemDarkMode, (dpi, isDarkMode) => new { dpi, isDarkMode })
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateContextMenu(x.dpi, x.isDarkMode))
            .DisposeWith(disposable);

        systemEvents.TabletMode
            .CombineLatest(systemEvents.SystemDarkMode, nativeUI.Dpi.Throttle(TimeSpan.FromSeconds(2)), (isTabletMode, isDarkMode, dpi) => new { isTabletMode, isDarkMode, dpi })
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateNotifyIcon(x.isTabletMode, x.isDarkMode, x.dpi))
            .DisposeWith(disposable);

        config.UserConfig.PropertyChanged
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(propertyName =>
            {
                if (propertyName == nameof(UserConfig.ShowBatteryChargeRate))
                {
                    UpdateMonitorings();
                }
            })
            .DisposeWith(disposable);

        battery.Rate
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(rate => UpdateBattery(rate))
            .DisposeWith(disposable);

        Observable.Interval(TimeSpan.FromSeconds(1))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(_ =>
            {
                if (notifyIcon?.ContextMenuStrip?.Visible == true && batteryMenuItem?.Visible == true)
                {
                    battery.Update();
                }
            })
            .DisposeWith(disposable);
    }

    private void UpdateMonitorings()
    {
        var isMonitoringEnabled = config.UserConfig.ShowBatteryChargeRate;

        if (batteryMenuItem is ToolStripLabel label)
        {
            label.Visible = config.UserConfig.ShowBatteryChargeRate;
        }

        if (monitoringSeparator != null)
        {
            monitoringSeparator.Visible = isMonitoringEnabled;
        }
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void Init()
    {
        notifyIcon = Create<NotifyIcon>().DisposeWith(disposable);
        notifyIcon.Click += (sender, e) => ShowMenu();

        notifyIcon.Text = $"{config.AppFileInfo.ProductName} ({config.AppFileInfo.ProductVersion})";

#if DEBUG
        notifyIcon.Text += " [DEBUG BUILD]";
#endif

        notifyIcon.Visible = true;
    }

    private void ShowMenu()
    {
        var methodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        methodInfo.Invoke(notifyIcon, null);
    }

    private async Task<ContextMenuStrip> InitContextMenu()
    {
        var highRefreshRateMenuItem = default(ToolStripItem);

        var menu = new ContextMenu(systemEvents);
        menu.AddMonitoringItem("").LinkAs(ref batteryMenuItem).DisposeWith(disposable);
        menu.AddSeparator().LinkAs(ref monitoringSeparator);

        menu.AddMenuItem(commandManager.Resolve<ToggleRefreshRateCommand>())
            .DisposeWith(disposable)
            .LinkAs(ref highRefreshRateMenuItem);

        menu.AddSeparator(highRefreshRateMenuItem).DisposeWith(disposable);

        menu.AddMenuItem(commandManager.Resolve<ToggleTouchPadCommand>()).DisposeWith(disposable);
        menu.AddMenuItem(commandManager.Resolve<ToggleBoostCommand>()).DisposeWith(disposable);
        menu.AddMenuItem(commandManager.Resolve<ToggleGpuCommand>()).DisposeWith(disposable);

        menu.AddSeparator().DisposeWith(disposable);

        menu.AddMenuItem(commandManager.Resolve<SettingsCommand>()).DisposeWith(disposable);
        menu.AddMenuItem(commandManager.Resolve<AboutCommand>()).DisposeWith(disposable);

        menu.AddSeparator().DisposeWith(disposable);

        menu.AddMenuItem(commandManager.Resolve<ExitCommand>()).DisposeWith(disposable);

        UpdateMonitorings();
        UpdateBattery(await battery.Rate.FirstAsync());

        return menu;
    }

    private void UpdateBattery(int rate)
    {
        if (batteryMenuItem != null)
        {
            batteryMenuItem.Text = $"Charge Rate: {rate / 1000f:N4} W";
        }
    }

    private async void UpdateContextMenu(int dpi, bool isDarkMode)
    {
        if (notifyIcon.ContextMenuStrip != null)
        {
            notifyIcon.ContextMenuStrip.Font?.Dispose();
            notifyIcon.ContextMenuStrip.Dispose();
        }

        notifyIcon.ContextMenuStrip = await InitContextMenu();
        notifyIcon.ContextMenuStrip.Font = new Font("Segoe UI Light", 14 * (dpi / 96f), FontStyle.Bold, GraphicsUnit.Pixel);
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


}
