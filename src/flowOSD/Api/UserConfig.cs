﻿/*  Copyright © 2021-2022, Albert Akhmetov <akhmetov@live.com>   
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
namespace flowOSD.Api;

using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

public sealed class UserConfig : INotifyPropertyChanged, IDisposable
{
    private Dictionary<PropertyChangedEventHandler, IDisposable> events;
    private Subject<string> propertyChangedSubject;
    private bool runAtStartup;
    private bool disableTouchPadInTabletMode;
    private bool controlDisplayRefreshRate;
    private bool highDisplayRefreshRateAC, highDisplayRefreshRateDC;

    private bool showPowerSourceNotification;
    private bool showBoostNotification;
    private bool showTouchPadNotification;
    private bool showDisplayRefreshRateNotification;

    private string auraCommand, fanCommand, rogCommand, copyCommand, pasteCommand;

    public UserConfig()
    {
        // Default values

        controlDisplayRefreshRate = true;
        highDisplayRefreshRateAC = true;
        highDisplayRefreshRateDC = false;
        disableTouchPadInTabletMode = true;

        showPowerSourceNotification = true;
        showBoostNotification = true;
        showTouchPadNotification = true;
        showDisplayRefreshRateNotification = true;

        events = new Dictionary<PropertyChangedEventHandler, IDisposable>();
        propertyChangedSubject = new Subject<string>();

        PropertyChanged = propertyChangedSubject.AsObservable();
    }

    void IDisposable.Dispose()
    {
        if (events == null)
        {
            return;
        }

        foreach (var d in events.Values)
        {
            d.Dispose();
        }

        events = null;
    }

    event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
    {
        add
        {
            events[value] = PropertyChanged.Subscribe(x => value(this, new PropertyChangedEventArgs(x)));
        }

        remove
        {
            if (events.ContainsKey(value))
            {
                events[value].Dispose();
                events.Remove(value);
            }
        }
    }

    [JsonIgnore]
    public IObservable<string> PropertyChanged { get; }

    [JsonIgnore]
    public bool RunAtStartup
    {
        get => runAtStartup;
        set => SetProperty(ref runAtStartup, value);
    }

    public bool DisableTouchPadInTabletMode
    {
        get => disableTouchPadInTabletMode;
        set => SetProperty(ref disableTouchPadInTabletMode, value);
    }

    public bool ControlDisplayRefreshRate
    {
        get => controlDisplayRefreshRate;
        set => SetProperty(ref controlDisplayRefreshRate, value);
    }

    public bool HighDisplayRefreshRateAC
    {
        get => highDisplayRefreshRateAC;
        set => SetProperty(ref highDisplayRefreshRateAC, value);
    }

    public bool HighDisplayRefreshRateDC
    {
        get => highDisplayRefreshRateDC;
        set => SetProperty(ref highDisplayRefreshRateDC, value);
    }

    public bool ShowPowerSourceNotification
    {
        get => showPowerSourceNotification;
        set => SetProperty(ref showPowerSourceNotification, value);
    }

    public bool ShowBoostNotification
    {
        get => showBoostNotification;
        set => SetProperty(ref showBoostNotification, value);
    }

    public bool ShowTouchPadNotification
    {
        get => showTouchPadNotification;
        set => SetProperty(ref showTouchPadNotification, value);
    }

    public bool ShowDisplayRateNotification
    {
        get => showDisplayRefreshRateNotification;
        set => SetProperty(ref showDisplayRefreshRateNotification, value);
    }

    public string AuraCommand
    {
        get => auraCommand;
        set => SetProperty(ref auraCommand, value);
    }

    public string FanCommand
    {
        get => fanCommand ;
        set => SetProperty(ref fanCommand, value);
    }

    public string RogCommand
    {
        get => rogCommand;
        set => SetProperty(ref rogCommand, value);
    }

    public string CopyCommand
    {
        get => copyCommand;
        set => SetProperty(ref copyCommand, value);
    }

    public string PasteCommand
    {
        get => pasteCommand;
        set => SetProperty(ref pasteCommand, value);
    }

    private void SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
    {
        if (!Equals(property, value))
        {
            property = value;
            propertyChangedSubject.OnNext(propertyName);
        }
    }
}
