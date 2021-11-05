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
    using static Native;

    sealed partial class Osd : IDisposable
    {
        private CompositeDisposable disposable = new CompositeDisposable();
        private OsdForm form;
        private SystemOsd systemOsd;

        public Osd()
        {
            form = new OsdForm().DisposeWith(disposable);

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

        public void Show(Data data)
        {
            systemOsd.Hide();

            form.Show(data);
        }

        public sealed class Data
        {
            public Data(Image image, string text)
            {
                Image = image;
                Text = text;
                Value = null;
            }

            public Data(Image image, double value)
            {
                Image = image;
                Text = null;
                Value = value;
            }

            public Image Image { get; }

            public string Text { get; }

            public double? Value { get; }

            public bool IsIndicator => Value != null;
        }

        private sealed class OsdForm : Form
        {
            private IDisposable hideTimer;
            private Data data;

            private SolidBrush accentBrush;

            public OsdForm()
            {
                FormBorderStyle = FormBorderStyle.None;
                BackColor = Color.FromArgb(18, 18, 18);
                ShowInTaskbar = false;

                Font = new Font("Segoe UI Light", 20, FontStyle.Bold);
            }

            public void Show(Data data)
            {
                this.data = data;

                hideTimer?.Dispose();
                hideTimer = null;

                UpdatePositionAndSize();

                Opacity = .95;
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
                if (accentBrush == null)
                {
                    accentBrush = new SolidBrush(GetAccentColor());
                }

                var scale = GetDpiScale();

                var thumbHeight = DpiScaleValue(11);

                var iRect = new Rectangle(
                    DpiScaleValue(27),
                    DpiScaleValue(20),
                    DpiScaleValue(11),
                    DpiScaleValue(79)
                );

                var fillHeight = (int)(iRect.Height * data.Value);

                g.FillRectangle(Brushes.Gray, iRect);
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
                g.DrawImage(
                    data.Image,
                    (Width - data.Image.Width) / 2,
                    Height - data.Image.Height - DpiScaleValue(16),
                    data.Image.Width,
                    data.Image.Height);
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

                base.OnClosed(e);
            }

            protected override void WndProc(ref System.Windows.Forms.Message message)
            {
                const int WM_DPICHANGED = 0x02E0;
                const int WM_WININICHANGE = 0x001A;

                if (message.Msg == WM_DPICHANGED)
                {
                    UpdatePositionAndSize();
                }

                if (message.Msg == WM_WININICHANGE && Marshal.PtrToStringUni(message.LParam) == "ImmersiveColorSet")
                {
                    InvalidateAccentColor();
                }

                base.WndProc(ref message);
            }

            private void InvalidateAccentColor()
            {
                if (accentBrush != null && accentBrush.Color != GetAccentColor())
                {
                    accentBrush?.Dispose();
                    accentBrush = null;
                }
            }

            private void UpdatePositionAndSize()
            {
                if (data == null)
                {
                    return;
                }

                Location = new Point(DpiScaleValue(50), DpiScaleValue(60));
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

            private double GetDpiScale()
            {
                return GetDpiForWindow(Handle) / 96d;
            }

            private int DpiScaleValue(int value)
            {
                return (int)Math.Ceiling(value * GetDpiScale());
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