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
using System.Management;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Api;
using flowOSD.Api.Hardware;
using Microsoft.Win32;

namespace flowOSD.Hardware.Optimization;

sealed class KeyboardBacklight : IKeyboardBacklight, IDisposable
{
    private const string BACKLIGHT_KEY = @"SOFTWARE\ASUS\ASUS System Control Interface\AsusOptimization\ASUS Keyboard Hotkeys";
    private const string BACKLIGHT_VALUE = "HidKeybdLightLevel";

    private BehaviorSubject<DeviceState> stateSubject;
    private BehaviorSubject<KeyboardBacklightLevel> levelSubject;

    private ManagementEventWatcher? watcher;

    public KeyboardBacklight()
    {
        stateSubject = new BehaviorSubject<DeviceState>(DeviceState.Enabled);
        levelSubject = new BehaviorSubject<KeyboardBacklightLevel>(KeyboardBacklightLevel.Off);

        Update();


        State = stateSubject.AsObservable();
        Level = levelSubject.AsObservable();

        var query = "SELECT * FROM RegistryValueChangeEvent WHERE Hive='HKEY_LOCAL_MACHINE' " +
            $"AND KeyPath='{BACKLIGHT_KEY.Replace("\\", "\\\\")}' AND ValueName='{BACKLIGHT_VALUE}'";

        watcher = new ManagementEventWatcher(query);
        watcher.EventArrived += OnWmiEvent;
        watcher.Start();
    }


    public IObservable<DeviceState> State { get; }

    public IObservable<KeyboardBacklightLevel> Level { get; }

    public void Dispose()
    {
        watcher?.Dispose();
        watcher = null;
    }

    public void LevelUp()
    {
        throw new NotSupportedException();
    }

    public void LevelDown()
    {
        throw new NotSupportedException();
    }

    public void SetLevel(KeyboardBacklightLevel value, bool force = false)
    {
        throw new NotSupportedException();
    }

    public void SetState(DeviceState value, bool force = false)
    {
        throw new NotSupportedException();
    }

    private void SetState(DeviceState value)
    {
        if (stateSubject.Value == value)
        {
            return;
        }

        stateSubject.OnNext(value);
    }

    private void Update()
    {
        using (var key = Registry.LocalMachine.OpenSubKey(BACKLIGHT_KEY, false))
        {
            if (key == null)
            {
                throw new ApplicationException("Registry Key for ASUS Optimization was not found.");
            }

            if (key == null || !int.TryParse(key.GetValue(BACKLIGHT_VALUE)?.ToString(), out int value))
            {
                throw new ApplicationException("Can't read the keyboard backlight value from Registry.");
            }

            stateSubject.OnNext(value == 1 ? DeviceState.Disabled : DeviceState.Enabled);

            if (value != 1 && Enum.IsDefined(typeof(KeyboardBacklightLevel), (byte)(value - 128)))
            {
                levelSubject.OnNext((KeyboardBacklightLevel)value - 128);
            }
        }
    }

    private void OnWmiEvent(object sender, EventArrivedEventArgs e)
    {
        Update();
    }
}
