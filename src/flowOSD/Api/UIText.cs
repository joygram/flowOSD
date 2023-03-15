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
using flowOSD.Api.Hardware;

namespace flowOSD.Api;

public static class UIText
{
    public static string MainUI_CpuBoost => "CPU Boost";

    public static string MainUI_HighRefreshRate => "High Refesh Rate";

    public static string MainUI_Gpu => "dGPU";

    public static string MainUI_TouchPad => "Touchpad";


    public static string Power_BatterySaver => "Battery Saver is on";

    public static string ToText(this PerformanceMode performanceMode)
    {
        switch (performanceMode)
        {
            case PerformanceMode.Silent:
                return "Silent";

            case PerformanceMode.Default:
                return "Default";

            case PerformanceMode.Turbo:
                return "Turbo";

            default:
                return "";
        }
    }

    public static string ToText(this PowerMode powerMode)
    {
        switch(powerMode)
        {
            case PowerMode.BestPowerEfficiency:
                return "Power Effeciency";

            case PowerMode.Balanced:
                return "Balanced";

            case PowerMode.BestPerformance:
                return "Performance";

            default:
                return "";
        }
    }
}
