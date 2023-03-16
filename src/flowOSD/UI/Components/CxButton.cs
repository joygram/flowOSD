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
namespace flowOSD.UI.Components;

using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reactive.Disposables;
using System.ComponentModel;
using static flowOSD.Extensions.Drawing;
using flowOSD.Extensions;

internal sealed class CxButton : ButtonBase
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private const float BACKGROUND_HOVER = -.1f;
    private const float BACKGROUND_PRESSED = -.2f;
    private const float BACKGROUND_DISABLED = .4f;
    private const float TEXT_HOVER = 0;
    private const float TEXT_PRESSED = -.3f;
    private const float TEXT_DISABLED = -.3f;
    private const float BORDER = .2f;
    private const int FOCUS_SPACE = 3;


    private ButtonState state;
    private CxTabListener? tabListener;

    private Pen? focusPen;

    private Color accentColor, textColor, textBrightColor;
    private bool isToggle, isTransparent, isChecked;

    private string icon;
    private Font? iconFont;

    private CxContextMenu? dropDownMenu;

    public CxButton()
    {
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

    public CxContextMenu? DropDownMenu
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
                disposable?.Remove(focusPen);
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

    public CxTabListener? TabListener
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

    [Bindable(true)]
    [DefaultValue(false)]
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

    [Bindable(true)]
    [DefaultValue(false)]
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

    [Bindable(true)]
    [DefaultValue(false)]
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

    private bool IsDropDownToggle => DropDownMenu != null && IsToggle;

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

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (ClientRectangle.Contains(e.Location))
        {
            if (IsDropDownToggle && e.Location.X > Width / 2)
            {
                State |= ButtonState.DropDownHover;
            }
            else
            {
                State &= ~ButtonState.DropDownHover;
            }
        }

        base.OnMouseMove(e);
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space || (Command != null && e.KeyCode == Keys.Enter))
        {
            State |= ButtonState.Pressed;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Space || (Command != null && e.KeyCode == Keys.Enter))
        {
            State &= ~ButtonState.Pressed;
        }

        if (e.KeyCode == Keys.Enter)
        {
            Command?.Execute(CommandParameter);

            return;
        }

        base.OnKeyUp(e);
    }

    protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
    {
        if ((e.KeyCode == Keys.Tab || e.KeyCode == Keys.Space) && TabListener != null)
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
            var clientRect = ClientRectangle;
            if (IsToggle)
            {
                clientRect = new Rectangle(
                    clientRect.X + clientRect.Width / 2,
                    clientRect.Y,
                    clientRect.Width / 2,
                    clientRect.Bottom);
            }

            if (clientRect.Contains(PointToClient(MousePosition)))
            {
                DropDownMenu.Show(this.PointToScreen(new Point(FOCUS_SPACE, this.Height)));

                if (TabListener?.ShowKeyboardFocus == true && DropDownMenu.Items.Count > 0)
                {
                    DropDownMenu.Items[0].Select();
                }

                return;
            }
        }

        if (Command == null && IsToggle)
        {
            IsChecked = !IsChecked;
        }

        base.OnClick(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

        e.Graphics.Clear(Color.Transparent);

        var baseColor = IsChecked ? AccentColor : BackColor;
        var backgroundColor = GetBackgroundColor(baseColor, !IsDropDownToggle);

        var drawingAreaRect = new Rectangle(
                FOCUS_SPACE,
                FOCUS_SPACE,
                Width - 1 - FOCUS_SPACE * 2,
                Height - 1 - FOCUS_SPACE * 2);

        if (backgroundColor != Color.Transparent || (State & ButtonState.MouseHover) == ButtonState.MouseHover)
        {
            using var brush = new SolidBrush(backgroundColor);
            e.Graphics.FillRoundedRectangle(brush, drawingAreaRect, 8);

            DrawDropDownHover(e, baseColor, drawingAreaRect);

            using var pen = new Pen(GetBorderColor(baseColor), 1);
            if (IsToggle && DropDownMenu != null)
            {
                e.Graphics.DrawLine(pen,
                    drawingAreaRect.X + drawingAreaRect.Width / 2,
                    drawingAreaRect.Y,
                    drawingAreaRect.X + drawingAreaRect.Width / 2,
                    drawingAreaRect.Bottom);
            }

            e.Graphics.DrawRoundedRectangle(pen, drawingAreaRect, 8);
        }

        if (TabListener?.ShowKeyboardFocus == true && Focused)
        {
            if (focusPen == null)
            {
                throw new InvalidOperationException("focusPen is null");
            }

            e.Graphics.DrawRoundedRectangle(focusPen, 0, 0, Width - 1, Height - 1, 8);
        }

        using var textBrush = new SolidBrush(GetTextColor(baseColor, (State & ButtonState.DropDownHover) == 0));

        var symbolSize = IconFont == null
            ? new Size(0, 0)
            : e.Graphics.MeasureString(Icon ?? string.Empty, IconFont);

        var textSize = e.Graphics.MeasureString(Text ?? string.Empty, Font);

        drawingAreaRect = DrawDropDownArrow(e, baseColor, drawingAreaRect, symbolSize, textSize);

        var symbolPoint = new PointF(
            drawingAreaRect.X + drawingAreaRect.Width / 2 - (symbolSize.Width + textSize.Width) / 2,
            (Height - symbolSize.Height) / 2 + 2);

        var textPoint = new PointF(
            symbolPoint.X + symbolSize.Width,
            (Height - textSize.Height) / 2);

        if (IconFont != null)
        {
            e.Graphics.DrawString(Icon, IconFont, textBrush, symbolPoint);
        }

        e.Graphics.DrawString(Text, Font, textBrush, textPoint);
    }

    private Rectangle DrawDropDownArrow(PaintEventArgs e, Color baseColor, Rectangle drawingAreaRect, SizeF symbolSize, SizeF textSize)
    {
        if (IconFont != null && DropDownMenu != null)
        {
            using var arrowBrush = new SolidBrush(
                GetTextColor(baseColor, !IsDropDownToggle || (State & ButtonState.DropDownHover) == ButtonState.DropDownHover));

            var arrowSymbol = "\ue972";
            var arrowSymbolSize = e.Graphics.MeasureString(Icon ?? string.Empty, IconFont);
            var arrowSymbolPoint = new PointF(
                drawingAreaRect.X + drawingAreaRect.Width * 3 / 4 - (symbolSize.Width + textSize.Width) / 2,
                (Height - symbolSize.Height) / 2 + 2);

            e.Graphics.DrawString(arrowSymbol, IconFont, arrowBrush, arrowSymbolPoint);

            drawingAreaRect.Width = IsToggle
                ? drawingAreaRect.Width / 2
                : (int)arrowSymbolPoint.X - drawingAreaRect.X;
        }

        return drawingAreaRect;
    }

    private void DrawDropDownHover(PaintEventArgs e, Color baseColor, Rectangle drawingAreaRect)
    {
        if (!IsDropDownToggle)
        {
            return;
        }

        using var hoveredBrush = new SolidBrush(GetBackgroundColor(baseColor, true));

        if ((State & ButtonState.DropDownHover) == ButtonState.DropDownHover)
        {
            e.Graphics.FillRoundedRectangle(
                hoveredBrush,
                new Rectangle(
                    drawingAreaRect.X + drawingAreaRect.Width / 2,
                    drawingAreaRect.Y,
                    drawingAreaRect.Width / 2,
                    drawingAreaRect.Height),
                8,
                Drawing.Corners.BottomRight | Drawing.Corners.TopRight);
        }
        else
        {
            e.Graphics.FillRoundedRectangle(
                hoveredBrush,
                new Rectangle(
                    drawingAreaRect.X,
                    drawingAreaRect.Y,
                    drawingAreaRect.Width / 2,
                    drawingAreaRect.Height),
                8,
                Drawing.Corners.BottomLeft | Drawing.Corners.TopLeft);
        }
    }

    private Color GetBorderColor(Color baseColor)
    {
        return baseColor.IsBright() ? baseColor.Luminance(-BORDER) : baseColor.Luminance(BORDER);
    }

    private Color GetTextColor(Color baseColor, bool isHoveredPart)
    {
        var isBright = baseColor.IsBright();

        if (!Enabled)
        {
            return isBright ? TextBrightColor.Luminance(-TEXT_DISABLED * 2) : TextColor.Luminance(TEXT_DISABLED);
        }
        else if (isHoveredPart && (State & ButtonState.Pressed) == ButtonState.Pressed)
        {
            return isBright ? TextBrightColor.Luminance(-TEXT_DISABLED * 2) : TextColor.Luminance(TEXT_PRESSED);
        }
        else
        {
            return isBright ? TextBrightColor : TextColor;
        }
    }

    private Color GetBackgroundColor(Color color, bool isHoveredPart)
    {
        if (!Enabled)
        {
            return color.IsBright()
                ? color.Luminance(-BACKGROUND_DISABLED / 2)
                : color.Luminance(BACKGROUND_DISABLED);
        }
        else if (isHoveredPart && (State & ButtonState.Pressed) == ButtonState.Pressed)
        {
            return color.Luminance(BACKGROUND_PRESSED);
        }
        else if (isHoveredPart && (State & ButtonState.MouseHover) == ButtonState.MouseHover)
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

    private void OnShowKeyboardFocusChanged(object? sender, EventArgs e)
    {
        Invalidate();
    }

    [Flags]
    private enum ButtonState
    {
        Default = 1,
        MouseHover = 2,
        Pressed = 4,
        DropDownHover = 8
    }
}
