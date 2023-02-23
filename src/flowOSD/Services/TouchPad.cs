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

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Api;
using Microsoft.Win32;
using static Native;

sealed class TouchPad : ITouchPad, IDisposable
{
    private static int WM_TOUCHPAD = (int)RegisterWindowMessage("Touchpad status reported from ATKHotkey");

    private const string TOUCHPAD_STATE_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PrecisionTouchPad\Status";
    private const string TOUCHPAD_STATE_VALUE = "Enabled";

    private readonly BehaviorSubject<bool> isEnabledSubject;
    private CompositeDisposable disposable = new CompositeDisposable();
    private IKeyboard keyboard;

    public TouchPad(IKeyboard keyboard, IMessageQueue messageQueue)
    {
        this.keyboard = keyboard;

        using (var key = Registry.CurrentUser.OpenSubKey(TOUCHPAD_STATE_KEY, false))
        {
            var isEnabled = key.GetValue(TOUCHPAD_STATE_VALUE)?.ToString() == "1";
            isEnabledSubject = new BehaviorSubject<bool>(isEnabled);
        }

        IsEnabled = isEnabledSubject.DistinctUntilChanged().AsObservable();

        messageQueue.Subscribe(WM_TOUCHPAD, ProcessMessage).DisposeWith(disposable);
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public IObservable<bool> IsEnabled
    {
        get;
    }

    public void Disable()
    {
        if (isEnabledSubject.Value)
        {
            Toggle();
        }
    }

    public void Enable()
    {
        if (!isEnabledSubject.Value)
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        keyboard.SendKeys(Keys.F24, Keys.ControlKey, Keys.LWin);
    }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_TOUCHPAD)
        {
            isEnabledSubject.OnNext((int)lParam == 1);
        }
    }
}