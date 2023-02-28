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

    private ICommandManager commandManager;
    private IConfig config;
    private IImageSource imageSource;

    public TrayIcon(
        IConfig config,
        IImageSource imageSource,
        ICommandManager commandManager,
        ISystemEvents systemEvents,
        IMessageQueue messageQueue)
    {
        if (systemEvents == null)
        {
            throw new ArgumentNullException(nameof(systemEvents));
        }

        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.imageSource = imageSource ?? throw new ArgumentNullException(nameof(imageSource));
        this.commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));

        Init();

        systemEvents.SystemUI
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateContextMenu(x))
            .DisposeWith(disposable);

        systemEvents.TabletMode
            .CombineLatest(systemEvents.SystemDarkMode, systemEvents.Dpi.Throttle(TimeSpan.FromSeconds(2)), (isTabletMode, isDarkMode, dpi) => new { isTabletMode, isDarkMode, dpi })
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateNotifyIcon(x.isTabletMode, x.isDarkMode, x.dpi))
            .DisposeWith(disposable);

        messageQueue
            .SubscribeToUpdateDpi(notifyIcon.ContextMenuStrip)
            .DisposeWith(disposable);
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void Init()
    {
        notifyIcon = Create<NotifyIcon>().DisposeWith(disposable);
        notifyIcon.MouseClick += (sender, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                commandManager.Resolve<MainUICommand>()?.Execute();
            }
        };

        notifyIcon.Text = $"{config.AppFileInfo.ProductName} ({config.AppFileInfo.ProductVersion})";

#if DEBUG
        notifyIcon.Text += " [DEBUG BUILD]";
#endif

        notifyIcon.ContextMenuStrip = InitContextMenu();
        notifyIcon.Visible = true;
    }

    private ContextMenuStrip InitContextMenu()
    {
        var menu = new CxContextMenu();

        menu.Font = new Font(UIParameters.FontName, 20, GraphicsUnit.Pixel);

        menu.AddMenuItem(commandManager.Resolve<MainUICommand>()).DisposeWith(disposable);

        menu.AddSeparator().DisposeWith(disposable);

        menu.AddMenuItem(commandManager.Resolve<SettingsCommand>()).DisposeWith(disposable);
        menu.AddMenuItem(commandManager.Resolve<AboutCommand>()).DisposeWith(disposable);

        menu.AddSeparator().DisposeWith(disposable);

        menu.AddMenuItem(commandManager.Resolve<ExitCommand>()).DisposeWith(disposable);

        return menu;
    }

    private void UpdateContextMenu(UIParameters uiParameters)
    {
        if (notifyIcon?.ContextMenuStrip is CxContextMenu menu)
        {
            menu.BackgroundColor = uiParameters.MenuBackgroundColor;
            menu.BackgroundHoverColor = uiParameters.MenuBackgroundHoverColor;
            menu.TextColor = uiParameters.MenuTextColor;
            menu.TextBrightColor = uiParameters.MenuTextBrightColor;
        }
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
