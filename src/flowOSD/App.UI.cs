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
namespace flowOSD
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Reactive.Subjects;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using static Extensions;
    using static Native;

    partial class App
    {
        private ToolStripMenuItem touchPadMenuItem, boostMenuItem, aboutMenuItem;
        private NotifyIcon notifyIcon;
        private UI ui;

        private void InitUI()
        {
            notifyIcon = Create<NotifyIcon>().DisposeWith(disposable);
            notifyIcon.Click += (sender, e) =>
            {
                var methodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                methodInfo.Invoke(notifyIcon, null);
            };

            notifyIcon.Text = AppAbout.Title;

            notifyIcon.ContextMenuStrip = Create<ContextMenuStrip>(x => x.RenderMode = ToolStripRenderMode.System).Add(
                Create<ToolStripMenuItem>(x =>
                    {
                        x.Padding = new Padding(0, 2, 0, 2);
                        x.Margin = new Padding(0, 8, 0, 8);
                        x.Click += (sender, e) => ToggleTouchPad();
                    }).DisposeWith(disposable).LinkAs(ref touchPadMenuItem),
                Create<ToolStripMenuItem>(x =>
                    {
                        x.Padding = new Padding(0, 2, 0, 2);
                        x.Margin = new Padding(0, 8, 0, 8);
                        x.Click += (sender, e) => ToggleBoost();
                    }).DisposeWith(disposable).LinkAs(ref boostMenuItem),
                Create<ToolStripSeparator>().DisposeWith(disposable),
                Create<ToolStripMenuItem>(x =>
                    {
                        x.Text = "About";
                        x.Padding = new Padding(0, 2, 0, 2);
                        x.Margin = new Padding(0, 8, 0, 8);
                        x.Click += (sender, e) => ShowAbout();
                    }).DisposeWith(disposable).LinkAs(ref aboutMenuItem),
                Create<ToolStripSeparator>().DisposeWith(disposable),
                Create<ToolStripMenuItem>(x =>
                    {
                        x.Text = "Exit";
                        x.Padding = new Padding(0, 2, 0, 2);
                        x.Margin = new Padding(0, 8, 0, 8);
                        x.Click += (sender, e) => Application.Exit();
                    }).DisposeWith(disposable)
                );

            ui = new UI(this).DisposeWith(disposable);

            notifyIcon.Visible = true;
            UpdateDpi(GetDpiForWindow(ui.Handle));
        }

        private void UpdateNotifyIcon(bool isTabletMode, bool isDarkMode)
        {
            var dpi = GetDpiForWindow(ui.Handle);

            if (isDarkMode)
            {
                notifyIcon.Icon = isTabletMode
                    ? images.GetIcon(Images.TabletWhite, dpi)
                    : images.GetIcon(Images.NotebookWhite, dpi);
            }
            else
            {
                notifyIcon.Icon = isTabletMode
                    ? images.GetIcon(Images.Tablet, dpi)
                    : images.GetIcon(Images.Notebook, dpi);
            }
        }

        private void UpdateDpi(int dpi)
        {
            notifyIcon.ContextMenuStrip.Font?.Dispose();
            notifyIcon.ContextMenuStrip.Font = new Font("Segoe UI", 12 * (dpi / 96f), GraphicsUnit.Pixel);

            UpdateNotifyIcon(this.isTabletModeSubject.Value, this.themeSubject.Value);
        }

        private sealed class UI : NativeWindow, IDisposable
        {
            private App owner;

            public UI(App owner)
            {
                this.owner = owner;
                AssignHandle(owner.notifyIcon.ContextMenuStrip.Handle);
            }

            ~UI()
            {
                Dispose(false);
            }

            void IDisposable.Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                ReleaseHandle();
            }

            protected override void WndProc(ref Message message)
            {
                const int WM_WININICHANGE = 0x001A;
                const int WM_DPICHANGED = 0x02E0;

                if (message.Msg == WM_WININICHANGE && Marshal.PtrToStringUni(message.LParam) == "ImmersiveColorSet")
                {
                    owner.themeSubject.OnNext(ShouldSystemUseDarkMode());
                }

                if (message.Msg == WM_WININICHANGE && Marshal.PtrToStringUni(message.LParam) == "ConvertibleSlateMode")
                {
                    owner.isTabletModeSubject.OnNext(GetSystemMetrics(SM_CONVERTIBLESLATEMODE) == 0);
                }

                if (message.Msg == WM_DPICHANGED)
                {
                    owner.dpiChangedSubject.OnNext((int)HiWord(message.WParam));
                }

                base.WndProc(ref message);
            }
        }
    }
}