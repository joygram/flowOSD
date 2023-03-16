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
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;
using flowOSD.Native;
using static flowOSD.Native.User32;
using static flowOSD.Native.Kernel32;
using System.Reactive.Linq;

namespace flowOSD.Services;

sealed class KeyboardBacklightService : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IKeyboardBacklight keyboardBacklight;
    private IKeyboard keyboard;
    private TimeSpan timeout;

    private volatile uint lastActivityTime;

    public KeyboardBacklightService(IKeyboardBacklight keyboardBacklight, IKeyboard keyboard, TimeSpan timeout)
    {
        this.keyboardBacklight = keyboardBacklight;
        this.keyboard = keyboard;
        this.timeout = timeout;

        this.keyboard.Activity
            .Subscribe(x =>
            {
                lastActivityTime = x;
                keyboardBacklight.SetState(DeviceState.Enabled);
            })
            .DisposeWith(disposable);

        Observable.Interval(TimeSpan.FromMilliseconds(1000))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => UpdateBacklightState())
            .DisposeWith(disposable);

    }

    public TimeSpan Timeout
    {
        get => timeout;
        set => timeout = value;
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public void ResetTimer()
    {
        lastActivityTime = GetTickCount();
    }

    private void UpdateBacklightState()
    {
        var lii = new LASTINPUTINFO();
        lii.cbSize = Marshal.SizeOf<LASTINPUTINFO>();

        if (GetLastInputInfo(ref lii))
        {
            var isIdle = timeout < TimeSpan.FromMilliseconds(GetTickCount() - Math.Max(lastActivityTime, lii.dwTime));

            keyboardBacklight.SetState(isIdle ? DeviceState.Disabled : DeviceState.Enabled);
        }
    }
}
