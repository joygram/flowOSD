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
using System.Management;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Api;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;

namespace flowOSD.Hardware;

sealed class AtkWmi : IDisposable, IAtkWmi, IKeyboard
{
    private const int AK_TABLET_STATE = 0xBD;
    private const int AK_CHARGER = 0x7B;

    private const uint DEVID_CHARGER = 0x0012006c;
    private const uint DEVID_TABLET = 0x00060077;

    private const int POWER_SOURCE_BATTERY = 0x00;
    private const int POWER_SOURCE_LOW = 0x22;
    private const int POWER_SOURCE_FULL = 0x2A;

    private IAtk atk;

    private ManagementEventWatcher? watcher;
    private readonly BehaviorSubject<ChargerType> chargerTypeSubject;
    private readonly BehaviorSubject<TabletMode> tabletModeSubject;
    private Subject<AtkKey> keyPressedSubject;

    public AtkWmi(IAtk atk)
    {
        this.atk = atk ?? throw new ArgumentNullException(nameof(atk));

        chargerTypeSubject = new BehaviorSubject<ChargerType>(GetChargerType());
        tabletModeSubject = new BehaviorSubject<TabletMode>(GetTabletMode());
        keyPressedSubject = new Subject<AtkKey>();

        ChargerType = chargerTypeSubject.AsObservable();
        TabletMode = tabletModeSubject.AsObservable();
        KeyPressed = keyPressedSubject.AsObservable();

        watcher = new ManagementEventWatcher("root\\wmi", "SELECT * FROM AsusAtkWmiEvent");
        watcher.EventArrived += OnWmiEvent;
        watcher.Start();
    }

    public void Dispose()
    {
        watcher?.Dispose();
        watcher = null;
    }

    public IObservable<ChargerType> ChargerType { get; }

    public IObservable<TabletMode> TabletMode { get; }

    IObservable<uint> IKeyboard.Activity { get; } = Observable.Empty<uint>();

    public IObservable<AtkKey> KeyPressed { get; }

    private void OnWmiEvent(object sender, EventArrivedEventArgs e)
    {
        var v = e.NewEvent.Properties.FirstOrDefault<PropertyData>(x => x.Name == "EventID")?.Value;
        if (v is not uint code)
        {
            return;
        }

        if (code >= byte.MinValue && code <= byte.MaxValue && Enum.IsDefined(typeof(AtkKey), (byte)code))
        {
            keyPressedSubject.OnNext((AtkKey)code);
            return;
        }

        switch (code)
        {
            case AK_TABLET_STATE:
                {
                    var tabletMode = GetTabletMode();

                    // Ignore rotated mode:
                    // - it reasonable in tablet mode (no touchpad manipulation is required)
                    // - it annoying when notebook mode (when device is tilted)

                    if (tabletMode != Api.Hardware.TabletMode.Rotated)
                    {
                        tabletModeSubject.OnNext(tabletMode);
                    }

                    break;
                }
            case AK_CHARGER:
                {
                    chargerTypeSubject.OnNext(GetChargerType());
                    break;
                }
        }
    }

    private TabletMode GetTabletMode()
    {
        return (TabletMode)atk.Get(DEVID_TABLET);
    }

    private ChargerType GetChargerType()
    {
        switch (atk.Get(DEVID_CHARGER))
        {
            case POWER_SOURCE_BATTERY:
                return Api.Hardware.ChargerType.None;

            case POWER_SOURCE_LOW:
                return Api.Hardware.ChargerType.LowPower;

            case POWER_SOURCE_FULL:
                return Api.Hardware.ChargerType.FullPower;

            default:
                throw new NotSupportedException("Charger type isn't supported");
        }
    }
}
