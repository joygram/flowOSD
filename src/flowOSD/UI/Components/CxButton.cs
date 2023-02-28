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

internal sealed class CxButton : ButtonBase
{
    private const float BACKGROUND_HOVER = -.1f;
    private const float BACKGROUND_PRESSED = -.2f;
    private const float BACKGROUND_DISABLED = -.05f;
    private const float TEXT_HOVER = 0;
    private const float TEXT_PRESSED = -.3f;
    private const float TEXT_DISABLED = .1f;
    private const float BORDER = -.1f;
    private const int FOCUS_SPACE = 3;

    private CompositeDisposable disposable;

    private ButtonState state;
    private CxTabListener tabListener;

    private Pen focusPen;

    private Color accentColor, textColor, textBrightColor;
    private bool isToggle, isTransparent, isChecked;

    private string icon;
    private Font iconFont;

    private CxContextMenu dropDownMenu;

    public CxButton()
    {
        disposable = new CompositeDisposable();

        isToggle = false;
        isChecked = false;
        isTransparent = false;

        FocusColor = Color.White;
        accentColor = Color.FromArgb(255, 25, 110, 191).Luminance(0.2f);

        textColor = Color.White;
        textBrightColor = Color.Black;

        state = 0;
        tabListener = null;

        icon = string.Empty;
        iconFont = null;
    }

    public CxContextMenu DropDownMenu
    {
        get => dropDownMenu;
        set
        {
            if (dropDownMenu == value)
            {
                return;
            }

            dropDownMenu = value;
            IsToggle = IsToggle && value != null;
        }
    }

    public Color AccentColor
    {
        get => accentColor;
        set
        {
            if (accentColor == value)
            {
                return;
            }

            accentColor = value;
            Invalidate();
        }
    }

    public Color FocusColor
    {
        get => (focusPen?.Color) ?? Color.Empty;
        set
        {
            if (focusPen?.Color == value)
            {
                return;
            }

            if (focusPen != null)
            {
                disposable.Remove(focusPen);
                focusPen.Dispose();
            }

            focusPen = new Pen(value, 2);
            Invalidate();
        }
    }

    public Color TextColor
    {
        get => textColor;
        set
        {
            if (textColor == value)
            {
                return;
            }

            textColor = value;
            Invalidate();
        }
    }

    public Color TextBrightColor
    {
        get => textBrightColor;
        set
        {
            if (textBrightColor == value)
            {
                return;
            }

            textBrightColor = value;
            Invalidate();
        }
    }

    public CxTabListener TabListener
    {
        get => tabListener;
        set
        {
            if (tabListener == value)
            {
                return;
            }

            if (tabListener != null)
            {
                tabListener.ShowKeyboardFocusChanged -= OnShowKeyboardFocusChanged;
            }

            tabListener = value;

            if (tabListener != null)
            {
                tabListener.ShowKeyboardFocusChanged += OnShowKeyboardFocusChanged;
            }

            Invalidate();
        }
    }

    public string Icon
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

    public Font IconFont
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

    public bool IsToggle
    {
        get => isToggle;
        set
        {
            if (isToggle == value)
            {
                return;
            }

            isToggle = value;

            if (!IsToggle && IsChecked)
            {
                IsChecked = false;
            }
            else
            {
                Invalidate();
            }
        }
    }

    public bool IsTransparent
    {
        get => isTransparent;
        set
        {
            if (isTransparent == value)
            {
                return;
            }

            isTransparent = value;
            Invalidate();
        }
    }

    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if ((!IsToggle && value) || isChecked == value)
            {
                return;
            }

            isChecked = value;
            Invalidate();
        }
    }

    private ButtonState State
    {
        get => state;
        set
        {
            if (state == value)
            {
                return;
            }

            state = value;
            Invalidate();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        State |= ButtonState.MouseHover;

        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        State &= ~ButtonState.MouseHover;

        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            State |= ButtonState.Pressed;
        }

        if (TabListener != null)
        {
            TabListener.ShowKeyboardFocus = false;
        };

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            State &= ~ButtonState.Pressed;
        }

        base.OnMouseUp(e);
    }

    protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
    {
        if (e.KeyCode == Keys.Tab && TabListener != null)
        {
            TabListener.ShowKeyboardFocus = true;
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            disposable?.Dispose();
            disposable = null;
        }

        base.Dispose(disposing);
    }

    protected override void OnClick(EventArgs e)
    {
        if (DropDownMenu != null)
        {
            DropDownMenu.Show(this.PointToScreen(new Point(FOCUS_SPACE, this.Height)));
        }
        else
        {
            IsChecked = !IsChecked;
        }

        base.OnClick(e);
    }

    private Rectangle GetClientRectangle()
    {
        return new Rectangle(
            FOCUS_SPACE,
            FOCUS_SPACE,
            Width - 1 - FOCUS_SPACE * 2,
            Height - 1 - FOCUS_SPACE * 2);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        e.Graphics.Clear(Color.Transparent);

        var baseColor = IsChecked ? AccentColor : BackColor;

        var backgroundColor = GetBackgroundColor(baseColor);
        var clientRect = GetClientRectangle();

        if (backgroundColor != Color.Transparent)
        {
            using var brush = new SolidBrush(backgroundColor);
            e.Graphics.FillRoundedRectangle(brush, clientRect, 8);

            using var pen = new Pen(baseColor.Luminance(BORDER), 1);
            e.Graphics.DrawRoundedRectangle(pen, clientRect, 8);
        }

        using var textBrush = new SolidBrush(GetTextColor(baseColor));

        var symbolSize = IconFont == null
            ? new Size(0, 0)
            : e.Graphics.MeasureString(Icon ?? string.Empty, IconFont);

        var textSize = e.Graphics.MeasureString(Text ?? string.Empty, Font);

        if (IconFont != null && DropDownMenu != null)
        {
            var arrowSymbol = "\ue972";
            var arrowSymbolSize = e.Graphics.MeasureString(Icon ?? string.Empty, IconFont);
            var arrowSymbolPoint = new PointF(
                clientRect.X + clientRect.Width * 3 / 4 - (symbolSize.Width + textSize.Width) / 2,
                (Height - symbolSize.Height) / 2 + 2);

            e.Graphics.DrawString(arrowSymbol, IconFont, textBrush, arrowSymbolPoint);

            clientRect.Width = (int)arrowSymbolPoint.X - clientRect.X;
        }

        var symbolPoint = new PointF(
            clientRect.X + clientRect.Width / 2 - (symbolSize.Width + textSize.Width) / 2,
            (Height - symbolSize.Height) / 2 + 2);

        var textPoint = new PointF(
            symbolPoint.X + symbolSize.Width,
            (Height - textSize.Height) / 2);

        if (IconFont != null)
        {
            e.Graphics.DrawString(Icon, IconFont, textBrush, symbolPoint);
        }

        e.Graphics.DrawString(Text, Font, textBrush, textPoint);

        if (TabListener.ShowKeyboardFocus && Focused)
        {
            e.Graphics.DrawRoundedRectangle(focusPen, 0, 0, Width - 1, Height - 1, 8);
        }
    }

    private Color GetTextColor(Color baseColor)
    {
        var isBright = baseColor.IsBright();

        if ((State & ButtonState.Pressed) == ButtonState.Pressed)
        {
            return isBright ? TextBrightColor : TextColor.Luminance(TEXT_PRESSED);
        }
        else
        {
            return isBright ? TextBrightColor : TextColor;
        }
    }

    private Color GetBackgroundColor(Color color)
    {
        if ((State & ButtonState.Pressed) == ButtonState.Pressed)
        {
            return color.Luminance(BACKGROUND_PRESSED);
        }
        else if ((State & ButtonState.MouseHover) == ButtonState.MouseHover)
        {
            return color.Luminance(BACKGROUND_HOVER);
        }
        else if (IsTransparent)
        {
            return Color.Transparent;
        }
        else
        {
            return color;
        }
    }

    private void OnShowKeyboardFocusChanged(object sender, EventArgs e)
    {
        Invalidate();
    }

    [Flags]
    private enum ButtonState
    {
        Default,
        MouseHover,
        Pressed,
    }
}
