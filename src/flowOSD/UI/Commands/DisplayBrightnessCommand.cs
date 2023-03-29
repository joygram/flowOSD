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
namespace flowOSD.UI.Commands;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.Api.Hardware;
using static flowOSD.Native.User32;

sealed class DisplayBrightnessCommand : CommandBase
{
    public const string UP = "up";
    public const string DOWN = "down";

    private static int WM_SHELLHOOK = RegisterWindowMessage("SHELLHOOK");

    private static readonly IList<ParameterInfo> parameters = new ReadOnlyCollection<ParameterInfo>(
        new ParameterInfo[]
        {
            new ParameterInfo(DOWN, "Down"),
            new ParameterInfo(UP, "Up")
        });

    private IConfig config;
    private IOsd osd;
    private IDisplayBrightness displayBrightness;

    public DisplayBrightnessCommand(IConfig config, IOsd osd, IDisplayBrightness displayBrightness)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.osd = osd ?? throw new ArgumentNullException(nameof(osd));
        this.displayBrightness = displayBrightness ?? throw new ArgumentNullException(nameof(displayBrightness));

        Text = "Display Brightness";
        Description = Text;
        Enabled = true;
    }

    public override string Name => nameof(DisplayBrightnessCommand);

    public override bool CanExecuteWithHotKey => true;

    public override IList<ParameterInfo> Parameters => parameters;

    public override void Execute(object? parameter = null)
    {
        if (parameter is string direction == false || !(direction != UP || direction != DOWN))
        {
            return;
        }

        if (direction == UP)
        {
            displayBrightness.LevelUp();
        }

        if (direction == DOWN)
        {
            displayBrightness.LevelDown();
        }

        var hostHandle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", "");
        if (hostHandle > 0 && (hostHandle = FindWindowEx(hostHandle, IntPtr.Zero, "ReBarWindow32", "")) > 0)
        {
            var shellHandle = FindWindowEx(hostHandle, IntPtr.Zero, "MSTaskSwWClass", null);
            if (shellHandle > 0)
            {
                SendMessage(shellHandle, WM_SHELLHOOK, 0x37, 0);
                return;
            }
        }

        // fail back:

        var icon = direction == DOWN
            ? UIImages.Hardware_BrightnessDown
            : UIImages.Hardware_BrightnessUp;

        osd.Show(new OsdData(icon, displayBrightness.GetLevel()));
    }
}
