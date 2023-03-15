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
using System.Reactive.Disposables;

internal class GeneralConfigPage : ConfigPageBase
{
    public GeneralConfigPage(IConfig config)
        : base(config)
    {
        Text = "General";

        AddConfig(
            "Run at logon",
            "Indicates whether the app starts when a user is logged on.",
            nameof(UserConfig.RunAtStartup));

        AddConfig(
            "Disable TouchPad in tablet mode",
            "Indicates whether TouchPad is disabled when the notebook goes into the tablet mode.",
            nameof(UserConfig.DisableTouchPadInTabletMode));

        AddConfig(
            "Control display refresh rate",
            "Indicates whether display refresh rate is dependent on the power source.",
            nameof(UserConfig.ControlDisplayRefreshRate));

        AddConfig(
            "Confirm GPU change",
            "Indicates whether confirmation is required for GPU change.",
            nameof(UserConfig.ConfirmGpuModeChange));

    }
}
