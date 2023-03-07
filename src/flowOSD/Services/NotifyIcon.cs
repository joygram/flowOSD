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
namespace flowOSD.Services;

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Api;
using static Native;

sealed partial class NotifyIcon : INotifyIcon, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();
    private Dictionary<int, MouseButtonAction> messageToMouseButtonAction;

    private static readonly Guid IconGuid = new Guid("EF27BC18-C13D-4056-BE35-3603AB766796");
    private static readonly int MessageId = 5800;

    private Subject<MouseButtonAction> mouseButtonAction;

    private IMessageQueue messageQueue;
    private string text;
    private Icon icon;

    public NotifyIcon(IMessageQueue messageQueue)
    {
        this.messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));

        messageToMouseButtonAction = new Dictionary<int, MouseButtonAction>();
        messageToMouseButtonAction[WM_LBUTTONDBLCLK] = Api.MouseButtonAction.LeftButtonDoubleClick;
        messageToMouseButtonAction[WM_LBUTTONDOWN] = Api.MouseButtonAction.LeftButtonDown;
        messageToMouseButtonAction[WM_LBUTTONUP] = Api.MouseButtonAction.LeftButtonUp;
        messageToMouseButtonAction[WM_RBUTTONDBLCLK] = Api.MouseButtonAction.RightButtonDoubleClick;
        messageToMouseButtonAction[WM_RBUTTONDOWN] = Api.MouseButtonAction.RightButtonDown;
        messageToMouseButtonAction[WM_RBUTTONUP] = Api.MouseButtonAction.RightButtonUp;
        messageToMouseButtonAction[WM_MBUTTONDBLCLK] = Api.MouseButtonAction.MiddleButtonDoubleClick;
        messageToMouseButtonAction[WM_MBUTTONDOWN] = Api.MouseButtonAction.MiddleButtonDown;
        messageToMouseButtonAction[WM_MBUTTONUP] = Api.MouseButtonAction.MiddleButtonUp;

        mouseButtonAction = new Subject<MouseButtonAction>();
        MouseButtonAction = mouseButtonAction.AsObservable();

        messageQueue.Subscribe(WM_TASKBARCREATED, ProcessMessage).DisposeWith(disposable);
        messageQueue.Subscribe(MessageId, ProcessMessage).DisposeWith(disposable);
    }

    void IDisposable.Dispose()
    {
        icon?.Dispose();
        icon = null;

        disposable?.Dispose();
        disposable = null;
    }

    public string Text
    {
        get => text;
        set
        {
            if (text == value)
            {
                return;
            }

            text = value;
            Update();
        }
    }

    public Icon Icon
    {
        get => icon;
        set
        {
            if (icon == value)
            {
                return;
            }

            icon = value;
            Update();
        }
    }

    public IObservable<MouseButtonAction> MouseButtonAction { get; }

    public Rectangle GetIconRectangle()
    {
        var notifyIcon = new NOTIFYICONIDENTIFIER();
        notifyIcon.cbSize = (uint)Marshal.SizeOf(notifyIcon.GetType());
        notifyIcon.hWnd = messageQueue.Handle;
        notifyIcon.uID = 1;

        if (Shell_NotifyIconGetRect(ref notifyIcon, out RECT rect) != S_OK)
        {
            return Rectangle.Empty;
        }
        else
        {
            return new Rectangle(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }
    }

    public void Show()
    {
        var notifyIconData = GetIconData();
        if (Icon == null)
        {
            notifyIconData.uFlags &= ~NIF_ICON;
        }

        Shell_NotifyIcon(NIM_ADD, ref notifyIconData);
    }

    public void Hide()
    {
        var notifyIconData = GetIconData();
        Shell_NotifyIcon(NIM_DELETE, ref notifyIconData);
    }

    private void Update()
    {
        var notifyIconData = GetIconData();
        if (Icon == null)
        {
            notifyIconData.uFlags &= ~NIF_ICON;
        }

        Shell_NotifyIcon(NIM_MODIFY, ref notifyIconData);
    }

    private NOTIFYICONDATA GetIconData()
    {
        return new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP | NIF_GUID,
            dwState = 0x0,
            hIcon = Icon?.Handler ?? IntPtr.Zero,
            hWnd = messageQueue.Handle,
            uCallbackMessage = MessageId,
            szTip = Text,
            uVersion = 5,
            guidItem = IconGuid
        };
    }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_TASKBARCREATED)
        {
            Show();
        }

        if (messageId != MessageId)
        {
            return;
        }

        var msg = lParam.Low();
        if (messageToMouseButtonAction.ContainsKey(msg))
        {
            mouseButtonAction.OnNext(messageToMouseButtonAction[msg]);
        }
    }
}
