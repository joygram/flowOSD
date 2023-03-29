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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.Api.Hardware;

sealed class KeyboardBacklightCommand : CommandBase
{
    public const string UP = "up";
    public const string DOWN = "down";

    private static readonly IList<ParameterInfo> parameters = new ReadOnlyCollection<ParameterInfo>(
        new ParameterInfo[]
        {
            new ParameterInfo(DOWN, "Down"),
            new ParameterInfo(UP, "Up")
        });

    private IConfig config;
    private IOsd osd;
    private IKeyboardBacklight keyboardBacklight;

    public KeyboardBacklightCommand(IConfig config, IOsd osd, IKeyboardBacklight keyboardBacklight)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.osd = osd ?? throw new ArgumentNullException(nameof(osd));
        this.keyboardBacklight = keyboardBacklight ?? throw new ArgumentNullException(nameof(keyboardBacklight));

        Text = "Keyboard Backlight";
        Description = Text;
        Enabled = true;
    }

    public override string Name => nameof(KeyboardBacklightCommand);

    public override bool CanExecuteWithHotKey => true;

    public override IList<ParameterInfo> Parameters => parameters;

    public override async void Execute(object? parameter = null)
    {
        if (parameter is string direction == false || !(direction != UP || direction != DOWN))
        {
            return;
        }

        if (direction == UP)
        {
            keyboardBacklight.LevelUp();
        }

        if (direction == DOWN)
        {
            keyboardBacklight.LevelDown();
        }

        var backlightLevel = await keyboardBacklight.Level.FirstOrDefaultAsync();

        var icon = direction == UP
            ? UIImages.Hardware_KeyboardLightDown
            : UIImages.Hardware_KeyboardLightUp;

        osd.Show(new OsdData(icon, (float)backlightLevel / (float)KeyboardBacklightLevel.High));
    }
}
