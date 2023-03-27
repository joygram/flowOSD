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

namespace flowOSD.Api.Hardware;

public interface IBattery
{
    string Name { get; }

    string ManufactureName { get; }

    uint DesignedCapacity { get; }

    uint FullChargedCapacity { get; }

    uint CycleCount { get; }

    IObservable<int> Rate { get; }

    IObservable<uint> Capacity { get; }

    IObservable<uint> EstimatedTime { get; }

    IObservable<BatteryPowerState> PowerState { get; }

    void Update();
}