/*  Copyright © 2021-2022, Albert Akhmetov <akhmetov@live.com>   
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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using flowOSD.Api;
using flowOSD.Services;
using flowOSD.UI.Commands;
using static Extensions;
using static Native;

sealed class TrayIcon : IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();
    private NotifyIcon notifyIcon;
    private ToolStripMenuItem highRefreshRateMenuItem, touchPadMenuItem, boostMenuItem, aboutMenuItem;

    private NativeUI nativeUI;
    private ICommandManager commandManager;
    private IConfig config;
    private IImageSource imageSource;
    private ISystemEvents systemEvents;

    public TrayIcon(NativeUI nativeUI, IConfig config, IImageSource imageSource, ICommandManager commandManager, ISystemEvents systemEvents)
    {
        this.nativeUI = nativeUI ?? throw new ArgumentNullException(nameof(nativeUI));

        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.imageSource = imageSource ?? throw new ArgumentNullException(nameof(imageSource));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));

        Init();

        BindCommandManager(commandManager);

        nativeUI.Dpi
            .Subscribe(dpi => UpdateDpi(dpi))
            .DisposeWith(disposable);

        systemEvents.TabletMode
            .CombineLatest(systemEvents.SystemDarkMode, nativeUI.Dpi.Throttle(TimeSpan.FromSeconds(2)), (isTabletMode, isDarkMode, dpi) => new { isTabletMode, isDarkMode, dpi })
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateNotifyIcon(x.isTabletMode, x.isDarkMode, x.dpi))
            .DisposeWith(disposable);
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void BindCommandManager(ICommandManager commandManager)
    {
        foreach (var item in notifyIcon.ContextMenuStrip.Items)
        {
            if (item is CommandMenuItem commandItem)
            {
                commandItem.CommandManager = commandManager;
            }
        }
    }

    private void Init()
    {
        notifyIcon = Create<NotifyIcon>().DisposeWith(disposable);
        notifyIcon.Click += (sender, e) => ShowMenu();

        notifyIcon.Text = $"{config.AppFileInfo.ProductName} ({config.AppFileInfo.ProductVersion})";

#if DEBUG
        notifyIcon.Text += " [DEBUG BUILD]";
#endif

        notifyIcon.ContextMenuStrip = InitContextMenu();


        notifyIcon.Visible = true;
    }

    private void ShowMenu()
    {
        var methodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        methodInfo.Invoke(notifyIcon, null);
    }

    private ContextMenuStrip InitContextMenu()
    {
        var menu = Create<Menu>(x =>
        {
            x.ForeColor = Color.White;
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

        SetCornerPreference(menu.Handle, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);
        menu.Opacity = 0.98;

        return menu;
    }

    private CommandMenuItem CreateCommandMenuItem(string commandName, object commandParameter = null, Action<CommandMenuItem> initializator = null)
    {
        var item = new CommandMenuItem();
        item.Padding = new Padding(0, 2, 0, 2);
        item.Margin = new Padding(0, 8, 0, 8);
        item.CommandName = commandName;
        item.CommandParameter = commandParameter;

        initializator?.Invoke(item);

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

    private class Menu : ContextMenuStrip
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            EnableAcrylic(this, Color.FromArgb(200, Color.Black));
            base.OnHandleCreated(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Transparent);
        }
    }
}
