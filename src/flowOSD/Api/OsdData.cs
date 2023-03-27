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

public class OsdData
{
    public OsdData(string text)
    {
        Icon = null;
        Text = text;
        Value = null;
    }

    public OsdData(string icon, string text)
    {
        Icon = icon;
        Text = text;
        Value = null;
    }

    public OsdData(string icon, double value)
    {
        Icon = icon;
        Text = null;
        Value = value;
    }

    public string? Icon { get; }

    public string? Text { get; }

    public double? Value { get; }

    public bool HasIcon => !string.IsNullOrEmpty(Icon);

    public bool IsIndicator => Value != null;
}