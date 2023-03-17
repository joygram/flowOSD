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
namespace flowOSD.UI.ConfigPages;

using flowOSD.Api;
using flowOSD.Extensions;
using flowOSD.UI.Components;
using System.Reactive.Disposables;

internal class NotificationsConfigPage : ConfigPageBase
{
    public NotificationsConfigPage(IConfig config, CxTabListener tabListener)
        :base(config, tabListener)
    {
        Text = "Notifications";

        AddConfig(
            UIImages.Hardware_AC,
            "Show power source notifications",
            nameof(UserConfig.ShowPowerSourceNotification));
        AddConfig(
            UIImages.Hardware_Cpu,
            "Show CPU boost mode notifications",
            nameof(UserConfig.ShowBoostNotification));
        AddConfig(
            UIImages.Performance_Turbo,
            "Show performance mode notifications",
            nameof(UserConfig.ShowPerformanceModeNotification));
        AddConfig(
            UIImages.Power_Balanced,
            "Show power mode notifications",
            nameof(UserConfig.ShowPowerModeNotification));
        AddConfig(
            UIImages.Hardware_TouchPad,
            "Show TouchPad notifications",
            nameof(UserConfig.ShowTouchPadNotification));
        AddConfig(
            UIImages.Hardware_Screen,
            "Show display refesh rate notifications",
            nameof(UserConfig.ShowDisplayRateNotification));
        AddConfig(
            UIImages.Hardware_Mic,
            "Show microphone status notifications",
            nameof(UserConfig.ShowMicNotification));
        AddConfig(
            UIImages.Hardware_Gpu,
            "Show dGPU notifications",
            nameof(UserConfig.ShowGpuNotification));
    }
}
