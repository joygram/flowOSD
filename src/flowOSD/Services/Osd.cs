/*  Copyright © 2021, Albert Akhmetov <akhmetov@live.com>   
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
namespace flowOSD.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;
    using flowOSD.Api;
    using static Native;

    sealed partial class Osd : IOsd, IDisposable
    {
        private CompositeDisposable disposable = new CompositeDisposable();
        private OsdForm form;
        private SystemOsd systemOsd;

        private Subject<OsdData> dataSubject;

        public Osd(ISystemEvents systemEvents, IImageSource imageSource)
        {
            dataSubject = new Subject<OsdData>();
            dataSubject
                .Where(x => !x.IsIndicator)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x =>
                {
                    systemOsd.Hide();
                    form.Show(x);
                })
                .DisposeWith(disposable);

            dataSubject
                .Where(x => x.IsIndicator)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x =>
                {
                    systemOsd.Hide();
                    form.Show(x);
                })
                .DisposeWith(disposable);

            form = new OsdForm(systemEvents, imageSource).DisposeWith(disposable);

            systemOsd = new SystemOsd().DisposeWith(disposable);
            systemOsd.IsVisible
                .Where(x => x)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => form.Visible = false)
                .DisposeWith(disposable);
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

            private SolidBrush accentBrush, grayBrush;

            public OsdForm(ISystemEvents systemEvents, IImageSource imageSource)
            {
                this.imageSource = imageSource;

                FormBorderStyle = FormBorderStyle.None;
                BackColor = Color.FromArgb(18, 18, 18);
                ShowInTaskbar = false;
                DoubleBuffered = true;

                Font = new Font("Segoe UI Light", 20, FontStyle.Bold);

                grayBrush = new SolidBrush(Color.FromArgb(106, 106, 106)).DisposeWith(disposable);

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

                Opacity = .96;
                Invalidate();
                Visible = true;

                hideTimer = Observable
                    .Timer(DateTimeOffset.Now.AddMilliseconds(2000), TimeSpan.FromMilliseconds(500 / 8))
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(t =>
                    {
                        Opacity -= .1;

                        if (Opacity <= 0)
                        {
                            Visible = false;
                        }
                    });
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
                var txtSize = g.MeasureString(data.Text, Font);
                g.DrawString(
                    data.Text,
                    Font,
                    Brushes.White,
                    DpiScaleValue(25),
                    (Size.Height - txtSize.Height) / 2
                );
            }

            private void DrawIndicator(Graphics g)
            {
                var thumbHeight = DpiScaleValue(11);

                var iRect = new RectangleF(
                    DpiScaleValue(27),
                    DpiScaleValue(20),
                    DpiScaleValue(11),
                    DpiScaleValue(79)
                );

                var fillHeight = iRect.Height * (float)(data.Value ?? 0);

                g.FillRectangle(grayBrush, iRect);
                g.FillRectangle(
                    accentBrush,
                    iRect.Left,
                    iRect.Bottom - fillHeight,
                    iRect.Width,
                    fillHeight
                );
                g.FillRectangle(
                    Brushes.White,
                    iRect.Left,
                    iRect.Bottom - Math.Max(thumbHeight, fillHeight),
                    iRect.Width,
                    thumbHeight
                );

                var image = imageSource.GetImage(data.ImageName, GetDpiForWindow(Handle));

                g.DrawImage(
                    image,
                    (Width - image.Width) / 2,
                    Height - image.Height - DpiScaleValue(16),
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

                accentBrush?.Dispose();
                accentBrush = null;

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
                if (accentBrush != null && accentBrush.Color != accentColor)
                {
                    accentBrush.Dispose();
                    accentBrush = null;
                }

                if (accentBrush == null)
                {
                    accentBrush = new SolidBrush(accentColor);
                }
            }

            private void UpdatePositionAndSize()
            {
                if (data == null)
                {
                    return;
                }

                Location = new Point(DpiScaleValue(49.9f), DpiScaleValue(60));
                if (data.IsIndicator)
                {
                    Size = new Size(DpiScaleValue(65), DpiScaleValue(140));
                }
                else
                {
                    using (var g = Graphics.FromHwnd(Handle))
                    {
                        var txtSize = g.MeasureString(data.Text, Font);
                        Size = new Size(
                            DpiScaleValue(25) * 2 + (int)txtSize.Width,
                            DpiScaleValue(62)
                        );
                    }
                }
            }

            private int DpiScaleValue(float value)
            {
                return (int)Math.Round(value * GetDpiForWindow(Handle) / 96.0, 0, MidpointRounding.AwayFromZero);
            }
        }

        private sealed class SystemOsd : IDisposable
        {
            const uint EVENT_OBJECT_CREATE = 0x8000;
            const uint EVENT_OBJECT_SHOW = 0x8002;
            const uint EVENT_OBJECT_HIDE = 0x8003;
            const uint EVENT_OBJECT_STATECHANGE = 0x800A;

            private Subject<bool> isVisibleSubject;

            private WINEVENTPROC proc;
            private IntPtr hookId;
            private IntPtr handle;

            public SystemOsd()
            {
                const uint WINEVENT_OUTOFCONTEXT = 0x0000;

                isVisibleSubject = new Subject<bool>();
                proc = new WINEVENTPROC(WinEventProc);

                hookId = SetWinEventHook(
                   EVENT_OBJECT_CREATE,
                   EVENT_OBJECT_STATECHANGE,
                   IntPtr.Zero,
                   proc,
                   (uint)GetShellProcessId(),
                   0,
                   WINEVENT_OUTOFCONTEXT);

                IsVisible = isVisibleSubject.AsObservable();
            }

            ~SystemOsd()
            {
                Dispose(false);
            }

            void IDisposable.Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public IObservable<bool> IsVisible { get; }

            public void Hide()
            {
                ShowWindow(handle, 0);
            }

            private void Dispose(bool disposing)
            {
                if (disposing)
                {
                    isVisibleSubject?.Dispose();
                    isVisibleSubject = null;
                }

                UnhookWinEvent(hookId);
            }

            private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
            {
                if (idObject != 0 || idChild != 0)
                {
                    return;
                }

                if (handle == IntPtr.Zero && (eventType == EVENT_OBJECT_CREATE || eventType == EVENT_OBJECT_SHOW))
                {
                    if (GetWindowClassName(hWnd) == "NativeHWNDHost")
                    {
                        handle = GetSystemOsdHandle();
                    }
                }

                if (handle == hWnd && eventType == EVENT_OBJECT_SHOW)
                {
                    isVisibleSubject.OnNext(true);
                }

                if (handle == hWnd && eventType == EVENT_OBJECT_HIDE)
                {
                    isVisibleSubject.OnNext(false);
                }
            }

            private static IntPtr GetSystemOsdHandle()
            {
                IntPtr hWndHost;
                while ((hWndHost = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "NativeHWNDHost", "")) != IntPtr.Zero)
                {
                    if (FindWindowEx(hWndHost, IntPtr.Zero, "DirectUIHWND", "") != IntPtr.Zero)
                    {
                        GetWindowThreadProcessId(hWndHost, out int pid);
                        if (Process.GetProcessById(pid).ProcessName.ToLower() == "explorer")
                        {
                            return hWndHost;
                        }
                    }
                }

                return IntPtr.Zero;
            }
        }
    }
}