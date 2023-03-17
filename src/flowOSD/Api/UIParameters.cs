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
namespace flowOSD.Api;

using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

public sealed class UIParameters
{
    public static string FontName => "Segoe UI";

    public static string IconFontName => "Segoe Fluent Icons";

    public bool IsDarkMode { get; private set; }

    public Color AccentColor { get; private set; }

    public Color FocusColor { get; private set; }

    public Color BackgroundColor { get; private set; }

    public Color TextColor { get; private set; }

    public Color TextGrayColor { get; private set; }

    public Color ButtonBackgroundColor { get; private set; }

    public Color ButtonTextColor { get; private set; }

    public Color ButtonTextBrightColor { get; private set; }

    public Color MenuBackgroundColor { get; private set; }

    public Color MenuBackgroundHoverColor { get; private set; }

    public Color MenuTextColor { get; private set; }

    public Color MenuTextBrightColor { get; private set; }

    public Color MenuTextDisabledColor { get; private set; }

    public Color OsdBackgroundColor { get; private set; }

    public Color OsdTextColor { get; private set; }

    public Color OsdIndicatorBackgroundColor { get; private set; }

    public Color PanelBackgroundColor { get; private set; }

    public Color NavigationMenuBackgroundHoverColor { get; private set; }


    public static UIParameters Create(Color accentColor, bool isDarkMode)
    {
        var p = new UIParameters
        {
            IsDarkMode = isDarkMode,
            AccentColor = accentColor,
        };

        return isDarkMode
            ? InitDarkParameters(p)
            : InitLightParameters(p);
    }

    private static UIParameters InitDarkParameters(UIParameters p)
    {
        p.BackgroundColor = Color.FromArgb(255, 32, 32, 32);
        p.TextColor = Color.FromArgb(255, 250, 250, 250);
        p.TextGrayColor = Color.FromArgb(255, 190, 190, 190);

        p.FocusColor = Color.FromArgb(255, 250, 250, 250);

        p.MenuBackgroundColor = Color.FromArgb(255, 45, 45, 45);
        p.MenuBackgroundHoverColor = p.AccentColor;
        p.MenuTextColor = Color.FromArgb(255, 250, 250, 250);
        p.MenuTextBrightColor = Color.FromArgb(255, 30, 30, 30);
        p.MenuTextDisabledColor = Color.FromArgb(255, 170, 170, 170);

        p.ButtonBackgroundColor = Color.FromArgb(255, 62, 62, 62);
        p.ButtonTextColor = Color.FromArgb(255, 250, 250, 250);
        p.ButtonTextBrightColor = Color.FromArgb(255, 30, 30, 30);

        p.PanelBackgroundColor = Color.FromArgb(255, 45, 45, 45);
        p.NavigationMenuBackgroundHoverColor = Color.FromArgb(255, 45, 45, 45);

        // Background 32 32 32
        // Panel 45 45 45
        // Panel Hover  50 50 50
        // Button  55 55 55 (62 on hovered panel) 
        // Button Hover 60 60 60 (67 on hovered)
        // Button Press 50 50 50 (56 on hovered panel)

        // Menu 45 45 45
        // Menu Hover 52 52 52
        // Menu Selected 56 56 56
        // Menu Pressed 56 56 56

        // Side Menu 45 45 45
        // Side Menu Hover 41 41 41
        // Side Menu Pressed 45 45 45

        // Font = 255 or 207 for grayed

        return p;
    }

    private static UIParameters InitLightParameters(UIParameters p)
    {
        p.BackgroundColor = Color.FromArgb(255, 243, 243, 243);
        p.TextColor = Color.FromArgb(255, 30, 30, 30);
        p.TextGrayColor = Color.FromArgb(255, 95, 95, 95);
        p.FocusColor = Color.FromArgb(255, 30, 30, 30);

        p.MenuBackgroundColor = Color.FromArgb(255, 249, 249, 249);
        p.MenuBackgroundHoverColor = p.AccentColor;
        p.MenuTextColor = Color.FromArgb(255, 250, 250, 250);
        p.MenuTextBrightColor = Color.FromArgb(255, 30, 30, 30);
        p.MenuTextDisabledColor = Color.FromArgb(255, 158, 158, 158);

        p.ButtonBackgroundColor = Color.FromArgb(255, 251, 251, 251);
        p.ButtonTextColor = Color.FromArgb(255, 250, 250, 250);
        p.ButtonTextBrightColor = Color.FromArgb(255, 30, 30, 30);

        p.PanelBackgroundColor = Color.FromArgb(255, 251, 251, 251);
        p.NavigationMenuBackgroundHoverColor = Color.FromArgb(255, 255, 255, 255);

        // Background 243 243 243
        // Panel 251 251 251 
        // Panel Hover  246 246 246
        // Button  254 254 254 (252 on hovered panel)
        // Button Hover 250 250 250 (247 on hovered panel)
        // Button Press 250 250 250 (247 on hovered panel)

        // Menu 249 249 249
        // Menu Hover 243 243 243
        // Menu Selected 240 240 240
        // Menu Pressed 240 240 240

        // Side Menu 234 234 234
        // Side Menu Hover 237 237 237
        // Side Menu Pressed 234 234 234

        // Font = 27 or 96 for grayed

        return p;
    }
}
