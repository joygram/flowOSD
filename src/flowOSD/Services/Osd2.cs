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

sealed partial class Osd2 : IOsd, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();
    private OsdForm form;

    private Subject<OsdData> dataSubject;

    public Osd2(ISystemEvents systemEvents, IImageSource imageSource)
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

        form = new OsdForm(systemEvents, imageSource).DisposeWith(disposable);
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

        private IImageSource imageSource;

        private Pen accentPen, grayPen;

        public OsdForm(ISystemEvents systemEvents, IImageSource imageSource)
        {
            this.imageSource = imageSource;

            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(18, 18, 18);
            ShowInTaskbar = false;
            DoubleBuffered = true;

            Font = new Font("Segoe UI Light", 20, FontStyle.Bold);

            grayPen = new Pen(Color.FromArgb(66, 66, 66), DpiScaleValue(6)).DisposeWith(disposable);
            grayPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            grayPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

            systemEvents.AccentColor
                .Subscribe(color => InvalidateAccentColor(color))
                .DisposeWith(disposable);
        }

        public void Show(OsdData data)
        {
            this.data = data;

            hideTimer?.Dispose();
            hideTimer = null;

            UpdatePositionAndSize();

            Opacity = 0.98;
            Invalidate();
            Visible = true;

            hideTimer = Observable
                .Timer(DateTimeOffset.Now.AddMilliseconds(1500), TimeSpan.FromMilliseconds(500 / 16))
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
            EnableAcrylic(this, Color.FromArgb(220, Color.Black));
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

        private void DrawText(Graphics g)
        {
            if (data.HasImage)
            {
                var image = imageSource.GetImage(data.ImageName, GetDpiForWindow(Handle));

                g.DrawImage(
                    image,
                    (Height - image.Height),
                    (Height - image.Height) / 2,
                    image.Width,
                    image.Height);
            }


            var x = data.HasImage ? DpiScaleValue(80) : DpiScaleValue(25);
            var txtSize = g.MeasureString(data.Text, Font);
            g.DrawString(
                data.Text,
                Font,
                Brushes.White,
                x,
                (Size.Height - txtSize.Height) / 2
            );
        }

        private void DrawIndicator(Graphics g)
        {
            var image = imageSource.GetImage(data.ImageName, GetDpiForWindow(Handle));
            var barWidth = Width - image.Width * 4;

            g.DrawLine(
                grayPen,
                new PointF(image.Width * 3, Height / 2),
                new PointF(image.Width * 3 + barWidth, Height / 2));

            var percent = (float)(data.Value ?? 0);

            if (percent > 0)
            {
                g.DrawLine(
                    accentPen,
                    new PointF(image.Width * 3, Height / 2),
                    new PointF(image.Width * 3 + barWidth * percent, Height / 2));
            }

            g.DrawImage(
                image,
                image.Width,
                (Height - image.Height) / 2,
                image.Width,
                image.Height);
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

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TOPMOST = 0x00000008;
                const int WS_EX_LAYERED = 0x00080000;
                const int WS_EX_NOACTIVATE = 0x08000000;

                var p = base.CreateParams;
                p.ExStyle = WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_NOACTIVATE;

                return p;
            }
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

            if (data.IsIndicator)
            {
                Size = new Size(DpiScaleValue(200), DpiScaleValue(50));
            }
            else
            {
                var x = data.HasImage ? DpiScaleValue(80) : DpiScaleValue(25);

                using (var g = Graphics.FromHwnd(Handle))
                {
                    var txtSize = g.MeasureString(data.Text, Font);
                    Size = new Size(
                        x + DpiScaleValue(25) + (int)txtSize.Width,
                        DpiScaleValue(65)
                    );
                }
            }

            Location = new Point(
                (Screen.PrimaryScreen.WorkingArea.Width - Size.Width) / 2,
                DpiScaleValue(32));
        }

        private int DpiScaleValue(float value)
        {
            return (int)Math.Round(value * GetDpiForWindow(Handle) / 96.0, 0, MidpointRounding.AwayFromZero);
        }
    }
}