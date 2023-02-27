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
    public bool IsDarkMode { get; private set; }

    public Color AccentColor { get; private set; }

    public string FontName { get; private set; }

    public string IconFontName { get; private set; }

    public Color FocusColor { get; private set; }

    public Color BackgroundColor { get; private set; }

    public Color TextColor { get; private set; }

    public Color ButtonBackgroundColor { get; private set; }

    public Color ButtonTextColor { get; private set; }

    public Color ButtonTextBrightColor { get; private set; }

    public Color MenuBackgroundColor { get; private set; }

    public Color MenuBackgroundHoverColor { get; private set; }

    public Color MenuTextColor { get; private set; }

    public Color MenuTextBrightColor { get; private set; }

    public Color OsdBackgroundColor { get; private set; }

    public Color OsdTextColor { get; private set; }

    public Color OsdIndicatorBackgroundColor { get; private set; }


    public static UIParameters Create(Color accentColor, bool isDarkMode)
    {
        var p = new UIParameters
        {
            IsDarkMode = isDarkMode,
            AccentColor = accentColor,
            FontName = "Segoe UI",
            IconFontName = "Segoe Fluent Icons"
        };

        return isDarkMode
            ? InitDarkParameters(p)
            : InitLightParameters(p);
    }

    private static UIParameters InitDarkParameters(UIParameters p)
    {
        p.BackgroundColor = Color.FromArgb(210, 44, 44, 44);

        p.MenuBackgroundColor = p.BackgroundColor;
        p.MenuBackgroundHoverColor = p.AccentColor;
        p.MenuTextColor = Color.White;
        p.MenuTextBrightColor = Color.Black;

        return p;
    }

    private static UIParameters InitLightParameters(UIParameters p)
    {
        p.BackgroundColor = Color.FromArgb(210, 249, 249, 249);

        return p;
    }
}
