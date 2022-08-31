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
namespace flowOSD.Services;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Api;
using static Native;

sealed class Atk : IAtk, IDisposable
{
    public readonly static int WM_ACPI = (int)RegisterWindowMessage("ACPI Notification through ATKHotkey from BIOS");

    private const int AK_BACKLIGHT_DOWN = 0xC5;
    private const int AK_BACKLIGHT_UP = 0xC4;
    private const int AK_AURA = 0xB3;
    private const int AK_FAN = 0xAE;
    private const int AK_TOUCHPAD = 0x6B;
    private const int AK_ROG = 0x38;
    private const int AK_MUTE_MIC = 0x7C;
    private const int AK_TABLET_STATE = 0xBD;
    private const int AK_FN_C = 0x9E;
    private const int AK_FN_V = 0x8A;

    private readonly Dictionary<int, AtkKey> codeToKey;
    private readonly Subject<AtkKey> keyPressedSubject;

    private CompositeDisposable disposable = new CompositeDisposable();

    public Atk(IMessageQueue messageQueue)
    {
        codeToKey = new Dictionary<int, AtkKey>();
        codeToKey[AK_BACKLIGHT_DOWN] = AtkKey.BacklightDown;
        codeToKey[AK_BACKLIGHT_UP] = AtkKey.BacklightUp;
        codeToKey[AK_AURA] = AtkKey.Aura;
        codeToKey[AK_FAN] = AtkKey.Fan;
        codeToKey[AK_TOUCHPAD] = AtkKey.TouchPad;
        codeToKey[AK_ROG] = AtkKey.Rog;
        codeToKey[AK_MUTE_MIC] = AtkKey.MuteMic;
        codeToKey[AK_FN_C] = AtkKey.Copy;
        codeToKey[AK_FN_V] = AtkKey.Paste;

        keyPressedSubject = new Subject<AtkKey>();

        KeyPressed = keyPressedSubject.Throttle(TimeSpan.FromMilliseconds(5)).AsObservable();

        messageQueue.Subscribe(WM_ACPI, ProcessMessage).DisposeWith(disposable);
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_ACPI)
        {
            var code = (int)wParam;

            if (codeToKey.ContainsKey(code))
            {
                keyPressedSubject.OnNext(codeToKey[code]);
            }
        }
    }

    public IObservable<AtkKey> KeyPressed
    {
        get;
    }
}