/*  Copyright © 2021, Albert Akhmetov <akhmetov@live.com>   
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
namespace flowOSD.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Microsoft.Win32;
    using static Native;

    class Atk : IMessageFilter, IDisposable
    {
        private static UInt32 WM_ACPI = RegisterWindowMessage("ACPI Notification through ATKHotkey from BIOS");
        private static UInt32 WM_TOUCHPAD = RegisterWindowMessage("Touchpad status reported from ATKHotkey");

        private const string TOUCHPAD_STATE_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\PrecisionTouchPad\Status";
        private const string TOUCHPAD_STATE_VALUE = "Enabled";

        private const int AK_BACKLIGHT_DOWN = 0xC5;
        private const int AK_BACKLIGHT_UP = 0xC4;
        private const int AK_AURA = 0xB3;
        private const int AK_FAN = 0xAE;
        private const int AK_TOUCHPAD = 0x6B;
        private const int AK_ROG = 0x38;
        private const int AK_MUTE_MIC = 0x7C;
        private const int AK_TABLET_STATE = 0xBD;

        private readonly Dictionary<int, Key> codeToKey;
        private readonly Subject<Key> keyPressedSubject;
        private readonly Subject<int> codeSubject;
        private readonly BehaviorSubject<bool> isTouchPadEnabledSubject;
        private readonly BehaviorSubject<bool> isTabletModeSubject;

        private CompositeDisposable disposable = new CompositeDisposable();

        public Atk()
        {
            codeToKey = new Dictionary<int, Key>();
            codeToKey[AK_BACKLIGHT_DOWN] = Key.BacklightDown;
            codeToKey[AK_BACKLIGHT_UP] = Key.BacklightUp;
            codeToKey[AK_AURA] = Key.Aura;
            codeToKey[AK_FAN] = Key.Fan;
            codeToKey[AK_TOUCHPAD] = Key.TouchPad;
            codeToKey[AK_ROG] = Key.Rog;
            codeToKey[AK_MUTE_MIC] = Key.MuteMic;

            keyPressedSubject = new Subject<Key>();
            codeSubject = new Subject<int>();

            using (var key = Registry.CurrentUser.OpenSubKey(TOUCHPAD_STATE_KEY, false))
            {
                var isEnabled = key.GetValue(TOUCHPAD_STATE_VALUE)?.ToString() == "1";
                isTouchPadEnabledSubject = new BehaviorSubject<bool>(isEnabled);
            }

            KeyPressed = keyPressedSubject.Throttle(TimeSpan.FromMilliseconds(5)).AsObservable();
            IsTouchPadEnabled = isTouchPadEnabledSubject.DistinctUntilChanged().AsObservable();
        }

        void IDisposable.Dispose()
        {
            disposable?.Dispose();
            disposable = null;
        }

        bool IMessageFilter.PreFilterMessage(ref Message m)
        {         
            if (m.Msg == WM_ACPI)
            {
                var code = (int)m.WParam;
                codeSubject.OnNext(code);

                if (codeToKey.ContainsKey(code))
                {
                    keyPressedSubject.OnNext(codeToKey[code]);
                }

                return true;
            }
            else if (m.Msg == WM_TOUCHPAD)
            {
                isTouchPadEnabledSubject.OnNext((int)m.LParam == 1);

                return true;
            }
            else
            {
                return false;
            }
        }

        public IObservable<Key> KeyPressed
        {
            get;
        }

        public IObservable<bool> IsTabletMode
        {
            get;
        }

        public IObservable<bool> IsTouchPadEnabled
        {
            get;
        }

        public enum Key
        {
            BacklightDown,
            BacklightUp,
            Aura,
            Fan,
            Rog,
            TouchPad,
            MuteMic,
        }
    }
}