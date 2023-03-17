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

namespace flowOSD.UI.Components;

internal sealed class CxLabel : Label
{
    private string? icon;
    private Font? iconFont;
    private bool useClearType;

    public CxLabel()
    {
        icon = null;
        iconFont = null;
        useClearType = false;
    }

    public string? Icon
    {
        get => icon;
        set
        {
            if (icon == value)
            {
                return;
            }

            icon = value;
            Invalidate();
        }
    }

    public Font? IconFont
    {
        get => iconFont;
        set
        {
            if (iconFont == value)
            {
                return;
            }

            iconFont = value;
            Invalidate();
        }
    }

    public bool UseClearType
    {
        get => useClearType;
        set
        {
            if (useClearType == value)
            {
                return;
            }

            useClearType = value;
            Invalidate();
        }
    }

    public CxTabListener? TabListener
    {
        get; set;
    }

    public override Size GetPreferredSize(Size proposedSize)
    {
        if (IsDisposed)
        {
            return Size.Empty;
        }

        using var g = Graphics.FromHwnd(Handle);

        var symbolSize = IconFont == null
            ? new SizeF(0, 0)
            : g.MeasureString(Icon ?? string.Empty, IconFont);

        var textSize = g.MeasureString(Text, Font);

        var totalSize = new Size(
            (int)(symbolSize.Width + textSize.Width),
            (int)Math.Max(symbolSize.Height, textSize.Height));

        return new Size(
            Padding.Left + Padding.Right + totalSize.Width,
            Padding.Top + Padding.Bottom + totalSize.Height);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (TabListener != null)
        {
            TabListener.ShowKeyboardFocus = false;
        }

        base.OnMouseClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
        e.Graphics.TextRenderingHint = UseClearType
            ? TextRenderingHint.ClearTypeGridFit
            : TextRenderingHint.AntiAliasGridFit;

        var symbolSize = IconFont == null
            ? new SizeF(0, 0)
            : e.Graphics.MeasureString(Icon ?? string.Empty, IconFont);
        var textSize = e.Graphics.MeasureString(Text, Font);
        var totalSize = new SizeF(
            symbolSize.Width + textSize.Width,
            Math.Max(symbolSize.Height, textSize.Height));

        var dY = (symbolSize.Height - textSize.Height) / 2;

        using var brush = new SolidBrush(ForeColor);

        var x = GetX(totalSize);
        var y = GetY(totalSize);

        if (IconFont != null)
        {
            e.Graphics.DrawString(Icon, IconFont, brush, x, y + Math.Max(0, -dY));
        }

        e.Graphics.DrawString(Text, Font, brush, x + symbolSize.Width, y + Math.Max(0, dY));
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
            case ContentAlignment.MiddleLeft:
                return (Height - textSize.Height) / 2;

            case ContentAlignment.BottomRight:
            case ContentAlignment.BottomCenter:
            case ContentAlignment.BottomLeft:
                return Height - textSize.Height - Padding.Bottom;

            default:
                return 0;
        }
    }
}
