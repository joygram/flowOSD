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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using flowOSD.Services;
    using static flowOSD.Extensions;
    using static Native;

    partial class App : IDisposable
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var program = new App())
            {
                Application.Run();
            }
        }

        private const int SM_CONVERTIBLESLATEMODE = 0x2003;


        private CompositeDisposable disposable = new CompositeDisposable();

        private Osd osd;
        private Keyboard keyboard;
        private PowerManagement powerManagement;
        private Atk atk;

        private Images images;
        private BehaviorSubject<bool> isTabletModeSubject;
        private BehaviorSubject<bool> themeChangedSubject;


        private App()
        {
            themeChangedSubject = new System.Reactive.Subjects.BehaviorSubject<bool>(ShouldSystemUseDarkMode());
            isTabletModeSubject = new BehaviorSubject<bool>(GetSystemMetrics(SM_CONVERTIBLESLATEMODE) == 0);

            osd = new Osd().DisposeWith(disposable);
            keyboard = new Keyboard();
            powerManagement = new PowerManagement().DisposeWith(disposable);
            atk = new Atk().AsMessageFilter();

            images = new Images().DisposeWith(disposable);
            images.Load();

            InitUI();

            powerManagement.IsBoostEnabled
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => boostMenuItem.Text = x ? "Disable Boost" : "Enable Boost")
                .DisposeWith(disposable);

            atk.KeyPressed
                .Where(x => x == Atk.Key.BacklightDown || x == Atk.Key.BacklightUp)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => osd.Show(new Osd.Data(images.Keyboard, keyboard.GetBacklight())))
                .DisposeWith(disposable);

            atk.IsTouchPadEnabled
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => touchPadMenuItem.Text = x ? "Disable TouchPad" : "Enable TouchPad")
                .DisposeWith(disposable);

            atk.KeyPressed
                .Where(x => x == Atk.Key.TouchPad)
                .CombineLatest(atk.IsTouchPadEnabled, (_, isEnabled) => isEnabled)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => osd.Show(new Osd.Data(images.Keyboard, x ? "TouchPad on" : "TouchPad off")))
                .DisposeWith(disposable);

            isTabletModeSubject
                .CombineLatest(themeChangedSubject, (isTabletMode, isDarkMode) => new { isTabletMode, isDarkMode })
                .Throttle(TimeSpan.FromMilliseconds(50))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => UpdateNotifyIcon(x.isTabletMode, x.isDarkMode))
                .DisposeWith(disposable);

            isTabletModeSubject
                .Throttle(TimeSpan.FromMilliseconds(50))
                .Subscribe(async x => UpdateTouchPadState(x))
                .DisposeWith(disposable);
        }

        void IDisposable.Dispose()
        {
            disposable?.Dispose();
            disposable = null;
        }

        private void ShowAbout()
        {
            AppAbout.ShowForm();
        }

        private void UpdateTouchPadState(bool isTabletMode)
        {
            if (isTabletMode)
            {
                DisableTouchPad();
            }
            else
            {
                EnableTouchPad();
            }
        }

        private async void EnableTouchPad()
        {
            if (!await atk.IsTouchPadEnabled.FirstAsync())
            {
                ToggleTouchPad();
            }
        }

        private async void DisableTouchPad()
        {
            if (await atk.IsTouchPadEnabled.FirstAsync())
            {
                ToggleTouchPad();
            }
        }

        private void ToggleTouchPad()
        {
            try
            {
                keyboard.SendKeys(Keys.F24, Keys.ControlKey, Keys.LWin);
            }
            catch (Exception ex)
            {
                // Program.LogException(ex);
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
                // Program.LogException(ex);
            }
        }
    }
}
