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
using static flowOSD.Native.User32;

sealed class NotifyIconUI : IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private NotifyIcon notifyIcon;
    private CxContextMenu contextMenu;

    public NotifyIconUI(
        IConfig config,
        IMessageQueue messageQueue,
        ISystemEvents systemEvents,
        ICommandManager commandManager,
        IAtkWmi atkWmi)
    {
        notifyIcon = new NotifyIcon(messageQueue);
        contextMenu = CreateContextMenu(commandManager);

        atkWmi.TabletMode
            .CombineLatest(
                systemEvents.SystemDarkMode,
                systemEvents.Dpi,
                (tabletMode, isDarkMode, dpi) => new { tabletMode, isDarkMode, dpi })
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateNotifyIcon(x.tabletMode, x.isDarkMode, x.dpi))
            .DisposeWith(disposable);

        systemEvents.SystemUI
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(UpdateContextMenu)
            .DisposeWith(disposable);

        notifyIcon.MouseButtonAction
            .Where(x => x == MouseButtonAction.LeftButtonUp)
            .Subscribe(x => commandManager.Resolve<MainUICommand>()?.Execute())
            .DisposeWith(disposable);

        notifyIcon.MouseButtonAction
            .Where(x => x == MouseButtonAction.RightButtonUp)
            .CombineLatest(systemEvents.PrimaryScreen, (_, screen) => screen)
            .Subscribe(ShowContextMenu)
            .DisposeWith(disposable);

        notifyIcon.Text = $"{config.AppFileInfo.ProductName} ({config.AppFileInfo.ProductVersion})";

#if DEBUG
        notifyIcon.Text += " [DEBUG BUILD]";
#endif

        notifyIcon.Show();
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void UpdateNotifyIcon(TabletMode tabletMode, bool isDarkMode, int dpi)
    {
        var iconName = tabletMode == TabletMode.Tablet || tabletMode == TabletMode.Tent ? "tablet" : "notebook";
        if (isDarkMode)
        {
            iconName += "-white";
        }

        notifyIcon.Icon?.Dispose();
        notifyIcon.Icon = Icon.LoadFromResource($"flowOSD.Resources.{iconName}.ico", dpi);
    }

    private void ShowContextMenu(Screen screen)
    {
        if (contextMenu == null)
        {
            return;
        }

        ShowAndActivate(contextMenu.Handle);

        var rectangle = notifyIcon.GetIconRectangle();

        if (rectangle.IsEmpty || screen.WorkingArea.Contains(rectangle))
        {
            // icon isn't pinned or we can't get rectangle

            var pos = GetCursorPos();
            contextMenu.Show(
                pos.X + contextMenu.Width < screen.WorkingArea.Width ? pos.X : pos.X - contextMenu.Width,
                pos.Y - contextMenu.Height);
        }
        else
        {
            contextMenu.Show(
                rectangle.Left + (rectangle.Width - contextMenu.Width) / 2,
                screen.WorkingArea.Bottom - contextMenu.Handle.DpiScale(10) - contextMenu.Height);
        }
    }

    private CxContextMenu CreateContextMenu(ICommandManager commandManager)
    {
        var menu = new CxContextMenu();
        menu.Font = new Font(UIParameters.FontName, 10, GraphicsUnit.Point);

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
        if (contextMenu is CxContextMenu menu)
        {
            menu.BackgroundColor = uiParameters.MenuBackgroundColor;
            menu.BackgroundHoverColor = uiParameters.MenuBackgroundHoverColor;
            menu.TextColor = uiParameters.MenuTextColor;
            menu.TextBrightColor = uiParameters.MenuTextBrightColor;
        }
    }
}
