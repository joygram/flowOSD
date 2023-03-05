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
namespace flowOSD.UI.Commands;

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using flowOSD.Api;
using flowOSD.UI.Components;

sealed class NotifyIconMenuCommand : CommandBase, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();
    private INotifyIcon notifyIcon;
    private ICommandManager commandManager;
    private IMessageQueue messageQueue;
    private ISystemEvents systemEvents;
    private CxContextMenu contextMenu;

    public NotifyIconMenuCommand(
        INotifyIcon notifyIcon,
        ICommandManager commandManager,
        IMessageQueue messageQueue,
        ISystemEvents systemEvents)
    {
        this.notifyIcon = notifyIcon ?? throw new ArgumentNullException(nameof(notifyIcon));
        this.commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
        this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));

        InitContextMenu();

        this.messageQueue
            .SubscribeToUpdateDpi(contextMenu)
            .DisposeWith(disposable);

        this.systemEvents.SystemUI
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(UpdateContextMenu)
            .DisposeWith(disposable);

        Text = "";
        Description = Text;
        Enabled = true;
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public override string Name => nameof(NotifyIconMenuCommand);

    public override bool CanExecuteWithHotKey => false;

    public override void Execute(object parameter = null)
    {
        ShowContextMenu();
    }

    private async void ShowContextMenu()
    {
        if (contextMenu == null)
        {
            return;
        }

        Native.SetForegroundWindow(messageQueue.Handle);

        var screen = await systemEvents.PrimaryScreen.FirstAsync();
        var rectangle = notifyIcon.GetIconRectangle();

        if (rectangle.IsEmpty || screen.WorkingArea.Contains(rectangle))
        {
            // icon isn't pinned or we can't get rectangle

            var pos = Native.GetCursorPos();
            contextMenu.Show(
                pos.X + contextMenu.Width < screen.WorkingArea.Width ? pos.X : pos.X - contextMenu.Width,
                pos.Y - contextMenu.Height);
        }
        else
        {
            contextMenu.Show(
                rectangle.Left + (rectangle.Width - contextMenu.Width) / 2,
                screen.Bounds.Bottom - messageQueue.Handle.DpiScale(10) - contextMenu.Height);
        }
    }

    private void InitContextMenu()
    {
        contextMenu = new CxContextMenu();
        contextMenu.Font = new Font(UIParameters.FontName, 20, GraphicsUnit.Pixel);

        contextMenu
            .AddMenuItem(commandManager.Resolve<MainUICommand>())
            .DisposeWith(disposable);
        contextMenu
            .AddSeparator()
            .DisposeWith(disposable);

        contextMenu
            .AddMenuItem(commandManager.Resolve<SettingsCommand>())
            .DisposeWith(disposable);
        contextMenu
            .AddMenuItem(commandManager.Resolve<AboutCommand>())
            .DisposeWith(disposable);
        contextMenu
            .AddSeparator()
            .DisposeWith(disposable);

        contextMenu
            .AddMenuItem(commandManager.Resolve<ExitCommand>())
            .DisposeWith(disposable);
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
