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
namespace flowOSD.Services;

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Api;
using static Native;

sealed partial class Osd : IOsd, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();
    private OsdForm form;

    private Subject<OsdData> dataSubject;

    public Osd(ISystemEvents systemEvents)
    {
        dataSubject = new Subject<OsdData>();
        dataSubject
            .Where(x => !x.IsIndicator)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x =>
            {
                form.Show(x);
            })
            .DisposeWith(disposable);

        dataSubject
            .Where(x => x.IsIndicator)
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x =>
            {
                form.Show(x);
            })
            .DisposeWith(disposable);

        form = new OsdForm(systemEvents).DisposeWith(disposable);
        SetCornerPreference(form.Handle, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public void Show(OsdData data)
    {
        this.dataSubject.OnNext(data);
    }

    private sealed class OsdForm : Form
    {
        private CompositeDisposable disposable = new CompositeDisposable();
        private IDisposable hideTimer;
        private OsdData data;

        private bool isDarkTheme;

        private Brush textBrush;
        private Pen accentPen, indicatorBackgroundPen, lightIndicatorBackgroundPen, darkIndicatorBackgroundPen;

        public OsdForm(ISystemEvents systemEvents)
        {
            isDarkTheme = false;

            FormBorderStyle = FormBorderStyle.None;

            ShowInTaskbar = false;
            DoubleBuffered = true;
            TopMost = true;

            Font = new Font("Segoe UI Light", DpiScaleValue(Parameters.TextValueHeight), FontStyle.Bold, GraphicsUnit.Pixel);

            darkIndicatorBackgroundPen = CreateIndicatorBackgroundPen(
                Parameters.IndicatorDarkBackgroundColor,
                DpiScaleValue(Parameters.IndicatorValueHeight)).DisposeWith(disposable);
            lightIndicatorBackgroundPen = CreateIndicatorBackgroundPen(
                Parameters.IndicatorLightBackgroundColor,
                DpiScaleValue(Parameters.IndicatorValueHeight)).DisposeWith(disposable);

            UpdateTheme();

            systemEvents.AccentColor
                .Subscribe(color => InvalidateAccentColor(color))
                .DisposeWith(disposable);

            systemEvents.SystemDarkMode
                .Subscribe(isDarkMode => IsDarkTheme = isDarkMode)
                .DisposeWith(disposable);
        }

        public bool IsDarkTheme
        {
            get { return isDarkTheme; }
            private set
            {
                isDarkTheme = value;

                UpdateTheme();
            }
        }

        public void Show(OsdData data)
        {
            this.data = data;

            hideTimer?.Dispose();
            hideTimer = null;

            UpdatePositionAndSize();

            Opacity = 1;
            Invalidate();
            Visible = true;

            hideTimer = Observable
                .Timer(DateTimeOffset.Now.AddMilliseconds(Parameters.Timeout), TimeSpan.FromMilliseconds(500 / 16))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(t =>
                {
                    Opacity -= .1;
                    Location = new Point(Location.X, Location.Y - 5);

                    if (Opacity <= 0)
                    {
                        Visible = false;
                    }
                });
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Transparent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (data.IsIndicator)
            {
                DrawIndicator(e.Graphics);
            }
            else
            {
                DrawText(e.Graphics);
            }

            base.OnPaint(e);
        }

        private static Pen CreateIndicatorBackgroundPen(Color color, int width)
        {
            var pen = new Pen(color, width);
            pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            return pen;
        }

        private static string GetImage(OsdData data)
        {
            switch (data.ImageName)
            {
                case Images.HiRefreshRate:
                case Images.LowRefreshRate:
                    return "\ue7f4";
                case Images.TouchPad:
                    return "\uefa5";
                case Images.BoostOn:
                    return "\uec4a";
                case Images.BoostOff:
                    return "\uec48";
                case Images.AC:
                    return "\ue83e";
                case Images.DC:
                    return "\ue83f";
                case Images.Mic:
                    return "\ue720";
                case Images.MicMuted:
                    return "\uf781";
                case Images.KeyboardBrightness:
                    return "\ued39";
                case Images.KeyboardLowerBrightness:
                    return "\ued3a";
                case Images.Gpu:
                    return "\uf211";

                default:
                    return string.Empty;
            }
        }

        private int DrawImage(Graphics g, int paddingLeft, int imageHeight, int separator)
        {
            var image = GetImage(data);

            using var font = new Font("Segoe Fluent Icons", imageHeight, GraphicsUnit.Pixel);

            var point = new Point(paddingLeft, (Height - imageHeight) / 2);

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.DrawString(image, font, textBrush, point);

            var imageWidth = g.MeasureString(image, font).Width;

            if (data.ImageName == Images.HiRefreshRate || data.ImageName == Images.LowRefreshRate)
            {
                using var textFont = new Font(
                    "Segoe UI",
                    DpiScaleValue(Parameters.TextImageHeight / 4f),
                    GraphicsUnit.Pixel);

                var text = data.ImageName == Images.HiRefreshRate ? "120Hz" : "60Hz";
                var textSize = g.MeasureString(text, textFont);

                var textPoint = new Point(
                    paddingLeft + (int)((imageWidth - textSize.Width) / 2),
                    Height / 2 - (int)(textSize.Height * 2 / 3) - DpiScaleValue(1));

                g.DrawString(text, textFont, textBrush, textPoint);

            }

            return (int)imageWidth + separator;
        }

        private void UpdateTheme()
        {
            indicatorBackgroundPen = IsDarkTheme ? darkIndicatorBackgroundPen : lightIndicatorBackgroundPen;
            textBrush = IsDarkTheme ? Brushes.White : Brushes.Black;

            var color = IsDarkTheme
                ? Parameters.DarkBackgroundColor
                : Parameters.LightBackgroundColor;

            EnableAcrylic(this, color);

            Invalidate();
        }

        private void DrawText(Graphics g)
        {
            var x = DpiScaleValue(Parameters.TextPadding.Left);

            if (data.HasImage)
            {
                x += DrawImage(g, x, DpiScaleValue(Parameters.TextImageHeight), DpiScaleValue(Parameters.TextSeparator));
            }

            var textSize = g.MeasureString(data.Text, Font);

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.DrawString(data.Text, Font, textBrush, x, (Size.Height - textSize.Height) / 2);
        }

        private void DrawIndicator(Graphics g)
        {
            var x = DpiScaleValue(Parameters.InidicatorPadding.Left);
            x += DrawImage(
                g,
                x,
                DpiScaleValue(Parameters.IndicatorImageHeight),
                DpiScaleValue(Parameters.IndicatorSeparator));

            var barWidth = DpiScaleValue(Parameters.IndicatorValueWidth);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.DrawLine(
                indicatorBackgroundPen,
                new PointF(x, Height / 2),
                new PointF(x + barWidth, Height / 2));

            var percent = (float)(data.Value ?? 0);
            if (percent > 0)
            {
                g.DrawLine(
                    accentPen,
                    new PointF(x, Height / 2),
                    new PointF(x + barWidth * percent, Height / 2));
            }
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (!Visible)
            {
                hideTimer?.Dispose();
                hideTimer = null;
            }

            base.OnVisibleChanged(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            hideTimer?.Dispose();
            hideTimer = null;

            accentPen?.Dispose();
            accentPen = null;

            disposable?.Dispose();
            disposable = null;

            base.OnClosed(e);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            UpdatePositionAndSize();

            base.OnDpiChanged(e);
        }

        private void InvalidateAccentColor(Color accentColor)
        {
            if (accentPen != null && accentPen.Color != accentColor)
            {
                accentPen.Dispose();
                accentPen = null;
            }

            if (accentPen == null)
            {
                accentPen = new Pen(accentColor, DpiScaleValue(6));
                accentPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                accentPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            }
        }

        private void UpdatePositionAndSize()
        {
            if (data == null)
            {
                return;
            }

            var imageHeight = DpiScaleValue(data.IsIndicator ? Parameters.IndicatorImageHeight : Parameters.TextImageHeight);
            var width = DpiScaleValue(data.IsIndicator ? Parameters.InidicatorPadding.Left : Parameters.TextPadding.Left);
            var height = data.IsIndicator
              ? DpiScaleValue(Parameters.InidicatorPadding.Top + Parameters.InidicatorPadding.Bottom + Parameters.IndicatorImageHeight)
              : DpiScaleValue(Parameters.TextPadding.Top + Parameters.TextPadding.Bottom + Parameters.TextImageHeight);

            if (data.IsIndicator || data.HasImage)
            {
                using var font = new Font("Segoe Fluent Icons", imageHeight, GraphicsUnit.Pixel);
                using var g = Graphics.FromHwnd(Handle);

                width += (int)g.MeasureString(GetImage(data), font).Width;
                width += DpiScaleValue(data.IsIndicator ? Parameters.IndicatorSeparator : Parameters.TextSeparator);
            }

            if (data.IsIndicator)
            {
                width += DpiScaleValue(Parameters.IndicatorValueWidth + Parameters.InidicatorPadding.Right);
            }
            else
            {
                using var g = Graphics.FromHwnd(Handle);

                width += (int)g.MeasureString(data.Text, Font).Width;
                width += DpiScaleValue(Parameters.TextPadding.Right);
            }

            Size = new Size(width, height);
            Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - Size.Width) / 2, DpiScaleValue(Parameters.PositionX));
        }

        private int DpiScaleValue(float value)
        {
            return (int)Math.Round(value * GetDpiForWindow(Handle) / 96.0, 0, MidpointRounding.AwayFromZero);
        }
    }

    private static class Parameters
    {
        public static Padding TextPadding { get; } = new Padding(12, 12, 24, 12);

        public static int TextImageHeight { get; } = 40;

        public static int TextValueHeight { get; } = 26;

        public static int TextSeparator { get; } = 6;

        public static Padding InidicatorPadding { get; } = new Padding(16, 16, 24, 16);

        public static int IndicatorImageHeight { get; } = 16;

        public static int IndicatorValueWidth { get; } = 136;

        public static int IndicatorValueHeight { get; } = 6;

        public static int IndicatorSeparator { get; } = 16;

        public static int Timeout = 1500;

        public static int PositionX = 32;

        public static Color IndicatorLightBackgroundColor { get; } = Color.FromArgb(125, 125, 125);

        public static Color IndicatorDarkBackgroundColor { get; } = Color.FromArgb(170, 170, 170);

        public static Color LightBackgroundColor { get; } = Color.FromArgb(210, 249, 249, 249);

        public static Color DarkBackgroundColor { get; } = Color.FromArgb(210, 44, 44, 44);
    }

    private sealed class Padding
    {
        public Padding(int all)
            : this(all, all, all, all)
        { }

        public Padding(int horizontal, int vertical)
            : this(horizontal, vertical, horizontal, vertical)
        { }

        public Padding(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Left { get; }

        public int Right { get; }

        public int Top { get; }

        public int Bottom { get; }
    }
}