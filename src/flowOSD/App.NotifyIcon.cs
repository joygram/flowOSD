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
    private void InitNotifyIcon()
    {
        notifyIcon = new NotifyIcon(messageQueue);

        atk.TabletMode
            .CombineLatest(
                systemEvents.SystemDarkMode,
                systemEvents.Dpi,
                (tabletMode, isDarkMode, dpi) => new { tabletMode, isDarkMode, dpi })
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateNotifyIcon(x.tabletMode, x.isDarkMode, x.dpi))
            .DisposeWith(disposable);

        notifyIcon.MouseButtonAction
            .Where(x => x == MouseButtonAction.LeftButtonUp)
            .Subscribe(x => commandManager.Resolve<MainUICommand>()?.Execute())
            .DisposeWith(disposable);

        notifyIcon.MouseButtonAction
            .Where(x => x == MouseButtonAction.RightButtonUp)
            .Subscribe(x => commandManager.Resolve<NotifyIconMenuCommand>()?.Execute())
            .DisposeWith(disposable);

        notifyIcon.Text = $"{config.AppFileInfo.ProductName} ({config.AppFileInfo.ProductVersion})";

#if DEBUG
        notifyIcon.Text += " [DEBUG BUILD]";
#endif

        notifyIcon.Show();
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
}
