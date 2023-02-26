using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using flowOSD.Api;

namespace flowOSD.UI.Components
{
    internal class CxButton : ButtonBase
    {
        private ButtonState state;
        private CxTabListener tabListener;
        private Color focusColor, accentColor;
        private bool isToggle, isTransparent, isChecked;

        private string symbol;
        private int symbolHeight;

        private object[] dropDownItems;

        public CxButton()
        {
            isToggle = false;
            isChecked = false;
            isTransparent = false;

            focusColor = Color.White;
            accentColor = Color.FromArgb(255, 25, 110, 191);

            state = 0;
            tabListener = null;

            symbol = string.Empty;
            symbolHeight = Height / 2;
        }

        public object[] DropDownItems
        {
            get => dropDownItems;
            set
            {
                if (value != null)
                {
                    IsToggle = false;
                }

                dropDownItems = value;
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

        public Color FocusColor
        {
            get => focusColor;
            set
            {
                if (focusColor == value)
                {
                    return;
                }

                focusColor = value;
                Invalidate();
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

        public string Symbol
        {
            get => symbol;
            set
            {
                if (symbol == value)
                {
                    return;
                }

                symbol = value;
                Invalidate();
            }
        }

        public int SymbolHeight
        {
            get => symbolHeight;
            set
            {
                if (symbolHeight == value)
                {
                    return;
                }

                symbolHeight = value;
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

        public ISystemEvents SystemEvents { get; set; }

        protected override void OnClick(EventArgs e)
        {
            if (dropDownItems != null)
            {
                var menu = new CxContextMenu(SystemEvents);
                foreach (var i in dropDownItems)
                {
                    menu.Items.Add(i.ToString()).Margin = new Padding(0, 8, 0, 8);

                }

                menu.Show(this.PointToScreen(new Point(3, this.Height)));
            }
            else
            {
                IsChecked = !IsChecked;
            }
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            const int fs = 3;

            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

            e.Graphics.Clear(Color.Transparent);

            var baseColor = IsChecked ? AccentColor : BackColor;
            var backgroundColor = GetBackgroundColor(baseColor);
            var clientRect = new Rectangle(fs, fs, Width - 1 - fs * 2, Height - 1 - fs * 2);


            if (backgroundColor != Color.Transparent)
            {
                using var brush = new SolidBrush(backgroundColor);
                e.Graphics.FillRoundedRectangle(brush, clientRect, 8);

                using var pen = new Pen(baseColor.Shade(0.2f), 1);
                e.Graphics.DrawRoundedRectangle(pen, clientRect, 8);
            }

            using var symbolFont = new Font("Segoe Fluent Icons", SymbolHeight, GraphicsUnit.Pixel);
            using var textBrush = new SolidBrush(GetTextColor(baseColor));

            var symbolSize = e.Graphics.MeasureString(Symbol ?? string.Empty, symbolFont);
            var textSize = e.Graphics.MeasureString(Text ?? string.Empty, Font);

            var isDropDown = dropDownItems != null;

            if (isDropDown)
            {
                var arrowSymbol = "\ue972";
                var arrowSymbolSize = e.Graphics.MeasureString(Symbol ?? string.Empty, symbolFont);
                var arrowSymbolPoint = new PointF(
                    clientRect.X + clientRect.Width * 3 / 4 - (symbolSize.Width + textSize.Width) / 2,
                    (Height - symbolSize.Height) / 2 + 2);

                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.DrawString(arrowSymbol, symbolFont, textBrush, arrowSymbolPoint);

                clientRect.Width = (int)arrowSymbolPoint.X - clientRect.X;
            }

            var symbolPoint = new PointF(
                clientRect.X + clientRect.Width / 2 - (symbolSize.Width + textSize.Width) / 2,
                (Height - symbolSize.Height) / 2 + 2);

            var textPoint = new PointF(
                symbolPoint.X + symbolSize.Width,
                (Height - textSize.Height) / 2);

            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            e.Graphics.DrawString(Symbol, symbolFont, textBrush, symbolPoint);

            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.DrawString(Text, Font, textBrush, textPoint);

            if (TabListener.ShowKeyboardFocus && Focused)
            {
                using var pen = new Pen(FocusColor, 2);
                e.Graphics.DrawRoundedRectangle(pen, 0, 0, Width - 1, Height - 1, 8);
            }
        }

        private Color GetTextColor(Color baseColor)
        {
            var isLight = baseColor.GetBrightness() > 0.3;

            if ((State & ButtonState.Pressed) == ButtonState.Pressed)
            {
                return isLight ? Color.Black.Tint(0.3f) : Color.White.Shade(0.3f);
            }
            else
            {
                return isLight ? Color.Black : Color.White;
            }
        }

        private Color GetBackgroundColor(Color color)
        {
            var isLight = color.GetBrightness() > 0.5;

            if ((State & ButtonState.Pressed) == ButtonState.Pressed)
            {
                return color;
            }
            else if ((State & ButtonState.MouseHover) == ButtonState.MouseHover)
            {
                return isLight ? color.Shade(.10f) : color.Tint(.35f);
            }
            else if (IsTransparent)
            {
                return Color.Transparent;
            }
            else
            {
                return isLight ? color.Shade(.20f) : color.Tint(.25f);
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
}
