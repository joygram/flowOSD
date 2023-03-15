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
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Api;
using flowOSD.Api.Hardware;

namespace flowOSD.Hardware;

sealed class KeyboardBacklight : IKeyboardBacklight
{
    public const int FEATURE_KBD_REPORT_ID = 0x5a;

    private HidDevice device;

    private bool isKeyboardBacklightActive;
    private TimeSpan backlightTimeout = TimeSpan.FromSeconds(30);

    private BehaviorSubject<DeviceState> stateSubject;
    private BehaviorSubject<KeyboardBacklightLevel> levelSubject;

    public KeyboardBacklight(HidDevice device, KeyboardBacklightLevel level)
    {
        this.device = device ?? throw new ArgumentNullException(nameof(device));

        stateSubject = new BehaviorSubject<DeviceState>(DeviceState.Enabled);
        levelSubject = new BehaviorSubject<KeyboardBacklightLevel>(level);

        State = stateSubject.AsObservable();
        Level = levelSubject.AsObservable();

        WriteLevel(level);

        ////Observable.Interval(TimeSpan.FromMilliseconds(500))
        ////    .ObserveOn(SynchronizationContext.Current)
        ////    .Subscribe(x => UpdateBacklightState())
        ////    .DisposeWith(disposable);
    }

    public IObservable<DeviceState> State { get; }

    public IObservable<KeyboardBacklightLevel> Level { get; }

    public void LevelUp()
    {
        var level = levelSubject.Value;

        if (level < KeyboardBacklightLevel.High)
        {
            SetLevel((KeyboardBacklightLevel)((byte)level + 1));
        }
    }

    public void LevelDown()
    {
        var level = levelSubject.Value;

        if (level > KeyboardBacklightLevel.Off)
        {
            SetLevel((KeyboardBacklightLevel)((byte)level - 1));
        }
    }

    public void SetLevel(KeyboardBacklightLevel value)
    {
        var isOk = WriteLevel(value);

        if (isOk)
        {
            levelSubject.OnNext(value);
        }
    }

    public void SetState(DeviceState value)
    {
        if (value == DeviceState.Disabled)
        {
            WriteLevel(KeyboardBacklightLevel.Off);
        }
        else
        {
            WriteLevel(levelSubject.Value);
        }

        stateSubject.OnNext(value);
    }

    private bool WriteLevel(KeyboardBacklightLevel value)
    {
        byte level;

        if (value < KeyboardBacklightLevel.Off)
        {
            level = (byte)KeyboardBacklightLevel.Off;
        }
        else if (value > KeyboardBacklightLevel.High)
        {
            level = (byte)KeyboardBacklightLevel.High;
        }
        else
        {
            level = (byte)value;
        }

        return device.WriteFeatureData(
            FEATURE_KBD_REPORT_ID,
            0xba,
            0xc5,
            0xc4,
            level);
    }

    //////private void UpdateBacklightState()
    //////{
    //////    var lii = new LASTINPUTINFO();
    //////    lii.cbSize = Marshal.SizeOf<LASTINPUTINFO>();

    //////    if (GetLastInputInfo(ref lii))
    //////    {
    //////        var isIdle = backlightTimeout < TimeSpan.FromMilliseconds(GetTickCount() - Math.Max(lastSpecialKeyTime, lii.dwTime));

    //////        if (isIdle && levelSubject.Value != KeyboardBacklightLevel.Off && isKeyboardBacklightActive)
    //////        {
    //////            isKeyboardBacklightActive = false;
    //////            WriteLevel(KeyboardBacklightLevel.Off);
    //////        }

    //////        if (!isIdle && levelSubject.Value > KeyboardBacklightLevel.Off && !isKeyboardBacklightActive)
    //////        {
    //////            isKeyboardBacklightActive = true;
    //////            WriteLevel(levelSubject.Value);
    //////        }
    //////    }
    //////}
}
