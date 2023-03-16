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
namespace flowOSD.Hardware;

using System.Management;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using flowOSD.Api;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;

sealed class Cpu : IDisposable, ICpu
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private readonly CountableSubject<uint> temperatureSubject;
    private IDisposable? updateSubscription;

    public Cpu()
    {
        temperatureSubject = new CountableSubject<uint>(GetTemperature());
        Temperature = temperatureSubject.AsObservable();

        updateSubscription = temperatureSubject.Count
            .Subscribe(sum =>
            {
                if (sum == 0 && updateSubscription != null)
                {
                    updateSubscription.Dispose();
                    updateSubscription = null;
                }

                if (sum > 0 && updateSubscription == null)
                {
                    updateSubscription = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ => UpdateTemperature());
                }
            })
            .DisposeWith(disposable);
    }

    public IObservable<uint> Temperature { get; }

    public void Dispose()
    {
        updateSubscription?.Dispose();
        updateSubscription = null;

        disposable?.Dispose();
        disposable = null;
    }

    private void UpdateTemperature()
    {
        temperatureSubject.OnNext(GetTemperature());
    }

    private uint GetTemperature()
    {
        var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation");
        foreach (ManagementObject obj in searcher.Get())
        {
            if (obj["Temperature"] is uint temperature)
            {
                return temperature - 273;
            }
        }

        return 0;
    }
}
