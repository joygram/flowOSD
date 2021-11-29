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
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Reflection;
    using System.Threading;
    using System.Windows.Forms;
    using flowOSD.Api;
    using static Extensions;
    using static Native;
    using flowOSD.UI;
    using flowOSD.Services;
    using System.Runtime.InteropServices;

    public class App : IDisposable
    {
        private CompositeDisposable disposable = new CompositeDisposable();

        private IMessageQueue messageQueue;
        private ISystemEvents systemEvents;
        private IImageSource imageSource;
        private IPowerManagement powerManagement;
        private Display display;
        private IAtk atk;
        private ITouchPad touchPad;
        private IKeyboard keyboard;
        private IOsd osd;

        private AboutUI aboutUI;

        private ToolStripMenuItem highRefreshRateMenuItem, touchPadMenuItem, boostMenuItem, aboutMenuItem;
        private NotifyIcon notifyIcon;
        private NativeUI nativeUI;

        public App()
        {
            ApplicationContext = new ApplicationContext().DisposeWith(disposable);

            aboutUI = new AboutUI().DisposeWith(disposable);
            Init();

            messageQueue = new MessageQueue().DisposeWith(disposable);
            imageSource = new ImageSource().DisposeWith(disposable);
            keyboard = new Keyboard();
            powerManagement = new PowerManagement().DisposeWith(disposable);

            systemEvents = new SystemEvents(messageQueue).DisposeWith(disposable);
            atk = new Atk(messageQueue).DisposeWith(disposable);
            touchPad = new TouchPad(keyboard, messageQueue).DisposeWith(disposable);
            osd = new Osd(systemEvents, imageSource).DisposeWith(disposable);

            display = new Display(messageQueue).DisposeWith(disposable);

            nativeUI = new NativeUI(notifyIcon.ContextMenuStrip.Handle, messageQueue).DisposeWith(disposable);

            powerManagement.IsBoost
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => boostMenuItem.Text = x ? "Disable Boost" : "Enable Boost")
                .DisposeWith(disposable);

            touchPad.IsEnabled
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => touchPadMenuItem.Text = x ? "Disable TouchPad" : "Enable TouchPad")
                .DisposeWith(disposable);

            atk.KeyPressed
                .Where(x => x == AtkKey.BacklightDown || x == AtkKey.BacklightUp)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => osd.Show(new OsdData(Images.Keyboard, keyboard.GetBacklight())))
                .DisposeWith(disposable);

            atk.KeyPressed
                .Where(x => x == AtkKey.TouchPad)
                .CombineLatest(touchPad.IsEnabled, (_, isEnabled) => isEnabled)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => osd.Show(new OsdData(Images.Keyboard, x ? "TouchPad on" : "TouchPad off")))
                .DisposeWith(disposable);

            systemEvents.TabletMode
                .CombineLatest(systemEvents.SystemDarkMode, nativeUI.Dpi.Throttle(TimeSpan.FromSeconds(2)), (isTabletMode, isDarkMode, dpi) => new { isTabletMode, isDarkMode, dpi })
                .Throttle(TimeSpan.FromMilliseconds(100))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => UpdateNotifyIcon(x.isTabletMode, x.isDarkMode, x.dpi))
                .DisposeWith(disposable);

            systemEvents.TabletMode
                .Throttle(TimeSpan.FromSeconds(2))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => UpdateTouchPadState(x))
                .DisposeWith(disposable);

            systemEvents.AppException
                .Subscribe(ex => TraceException(ex, "Unhandled application exception."))
                .DisposeWith(disposable);

            nativeUI.Dpi
                .Subscribe(dpi => UpdateDpi(dpi))
                .DisposeWith(disposable);

            display.IsHighRefreshRateSupported
                    .Throttle(TimeSpan.FromMilliseconds(200))
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(x =>
                    {
                        highRefreshRateMenuItem.Visible = x;
                        if (highRefreshRateMenuItem.Tag is ToolStripItem separator)
                        {
                            separator.Visible = x;
                        }
                    })
                    .DisposeWith(disposable);

            display.IsHighRefreshRate
                    .Throttle(TimeSpan.FromMilliseconds(200))
                        .ObserveOn(SynchronizationContext.Current)
                        .Subscribe(x => highRefreshRateMenuItem.Text = x ? "Disable High Refresh Rate" : "Enable High Refresh Rate")
                        .DisposeWith(disposable);
        }

        void IDisposable.Dispose()
        {
            disposable?.Dispose();
            disposable = null;
        }

        public ApplicationContext ApplicationContext { get; }

        private void Init()
        {
            notifyIcon = Create<NotifyIcon>().DisposeWith(disposable);
            notifyIcon.Click += (sender, e) =>
            {
                var methodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                methodInfo.Invoke(notifyIcon, null);
            };

            notifyIcon.Text = aboutUI.AppTitle;

            notifyIcon.ContextMenuStrip = Create<ContextMenuStrip>(x => x.RenderMode = ToolStripRenderMode.System).Add(
                Create<ToolStripMenuItem>(x =>
                    {
                        x.Padding = new Padding(0, 2, 0, 2);
                        x.Margin = new Padding(0, 8, 0, 8);
                        x.Click += (sender, e) => ToggleRefreshRate();
                    }).DisposeWith(disposable).LinkAs(ref highRefreshRateMenuItem),
                Create<ToolStripSeparator>(x => highRefreshRateMenuItem.Tag = x).DisposeWith(disposable),
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

            notifyIcon.Visible = true;
        }

        private void UpdateNotifyIcon(bool isTabletMode, bool isDarkMode, int dpi)
        {
            notifyIcon.Icon = null;

            if (isDarkMode)
            {
                notifyIcon.Icon = isTabletMode
                    ? imageSource.GetIcon(Images.TabletWhite, dpi)
                    : imageSource.GetIcon(Images.NotebookWhite, dpi);
            }
            else
            {
                notifyIcon.Icon = isTabletMode
                    ? imageSource.GetIcon(Images.Tablet, dpi)
                    : imageSource.GetIcon(Images.Notebook, dpi);
            }
        }

        private void UpdateDpi(int dpi)
        {
            notifyIcon.ContextMenuStrip.Font?.Dispose();
            notifyIcon.ContextMenuStrip.Font = new Font("Segoe UI", 12 * (dpi / 96f), GraphicsUnit.Pixel);
        }

        private void ToggleRefreshRate()
        {
            try
            {
                display.ToggleRefreshRate();
            }
            catch (Exception ex)
            {
                TraceException(ex, "Error is occurred while toggling display refresh rate (UI).");
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleBoost()
        {
            try
            {
                powerManagement.ToggleBoost();
            }
            catch (Exception ex)
            {
                TraceException(ex, "Error is occurred while toggling CPU boost mode (UI).");
            }
        }

        private void ToggleTouchPad()
        {
            try
            {
                touchPad.Toggle();
            }
            catch (Exception ex)
            {
                TraceException(ex, "Error is occurred while toggling TouchPad state (UI).");
            }
        }

        private void ShowAbout()
        {
            aboutUI.Show();
        }

        private void Exit()
        {
            Application.Exit();
        }

        private void UpdateTouchPadState(bool isTabletMode)
        {
            try
            {
                if (isTabletMode)
                {
                    touchPad.Disable();
                }
                else
                {
                    touchPad.Enable();
                }
            }
            catch (Exception ex)
            {
                TraceException(ex, "Error is occurred while toggling TouchPad state (Auto).");
            }
        }
    }
}