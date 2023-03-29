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

namespace flowOSD.Api.Configs;

using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using flowOSD.Api.Hardware;

public abstract class ConfigBase : INotifyPropertyChanged, IDisposable
{
    private Dictionary<PropertyChangedEventHandler, IDisposable>? events;
    private Subject<string?> propertyChangedSubject;

    public ConfigBase()
    {
        events = new Dictionary<PropertyChangedEventHandler, IDisposable>();
        propertyChangedSubject = new Subject<string?>();

        PropertyChanged = propertyChangedSubject.AsObservable();
    }

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add
        {
            if (events == null || value == null)
            {
                return;
            }

            events[value] = PropertyChanged.Subscribe(x => value(this, new PropertyChangedEventArgs(x)));
        }

        remove
        {
            if (events == null || value == null)
            {
                return;
            }

            if (events.ContainsKey(value))
            {
                events[value].Dispose();
                events.Remove(value);
            }
        }
    }

    [JsonIgnore]
    public IObservable<string?> PropertyChanged { get; }

    public virtual void Dispose()
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

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        propertyChangedSubject.OnNext(propertyName);
    }

    protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(property, value))
        {
            property = value;
            OnPropertyChanged(propertyName);
        }
    }
}
