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
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reactive.Disposables;
using System.Linq;
using flowOSD.Api;
using System.Reactive.Linq;
using static flowOSD.Native;
using System.Windows.Input;

namespace flowOSD.UI.Components;

internal sealed class CxLabel : Label
{
    public CxLabel()
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        e.Graphics.Clear(Color.Transparent);

        var textSize = e.Graphics.MeasureString(Text, Font);

        using var brush = new SolidBrush(ForeColor);
        e.Graphics.DrawString(Text, Font, brush, GetX(textSize), GetY(textSize));
    }

    private float GetX(SizeF textSize)
    {
        switch (TextAlign)
        {
            case ContentAlignment.BottomLeft:
            case ContentAlignment.MiddleLeft:
            case ContentAlignment.TopLeft:
                return Padding.Left;

            case ContentAlignment.BottomCenter:
            case ContentAlignment.MiddleCenter:
            case ContentAlignment.TopCenter:
                return (Width - textSize.Width) / 2;

            case ContentAlignment.BottomRight:
            case ContentAlignment.MiddleRight:
            case ContentAlignment.TopRight:
                return Width - textSize.Width - Padding.Right;

            default:
                return 0;
        }
    }

    private float GetY(SizeF textSize)
    {
        switch (TextAlign)
        {
            case ContentAlignment.TopRight:
            case ContentAlignment.TopCenter:
            case ContentAlignment.TopLeft:
                return Padding.Top;

            case ContentAlignment.MiddleCenter:
            case ContentAlignment.MiddleRight:
                return (Height - textSize.Height) / 2;

            case ContentAlignment.BottomRight:
            case ContentAlignment.BottomCenter:
            case ContentAlignment.BottomLeft:
            case ContentAlignment.MiddleLeft:
                return Height - textSize.Height - Padding.Bottom;

            default:
                return 0;
        }
    }
}
