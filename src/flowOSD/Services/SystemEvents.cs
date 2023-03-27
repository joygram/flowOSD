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

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using flowOSD.Api;

using static Native.UxTheme;
using static Native.User32;
using static Native.Messages;
using flowOSD.Extensions;
using Microsoft.Win32;

sealed partial class SystemEvents : ISystemEvents, IDisposable
{
    private const string PERSONALIZE_KEY = "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize";
    private const string PERSONALIZE_APP_VALUE = "AppsUseLightTheme";
    private const string PERSONALIZE_SYSTEM_VALUE = "SystemUsesLightTheme";

    private CompositeDisposable? disposable = new CompositeDisposable();

    private BehaviorSubject<bool> systemDarkModeSubject;
    private BehaviorSubject<bool> appsDarkModeSubject;
    private BehaviorSubject<Color> accentColorSubject;
    private BehaviorSubject<Screen?> primaryScreenSubject;
    private BehaviorSubject<int> dpiSubject;
    private BehaviorSubject<UIParameters> systemUISubject, appUISubject;

    public SystemEvents(IMessageQueue messageQueue)
    { 
        systemDarkModeSubject = new BehaviorSubject<bool>(IsSystemUseDarkMode());
        appsDarkModeSubject = new BehaviorSubject<bool>(IsAppUseDarkMode());
        accentColorSubject = new BehaviorSubject<Color>(GetAccentColor());

        primaryScreenSubject = new BehaviorSubject<Screen?>(Screen.PrimaryScreen);
        dpiSubject = new BehaviorSubject<int>(GetDpiForWindow(messageQueue.Handle));

        systemUISubject = new BehaviorSubject<UIParameters>(
            UIParameters.Create(accentColorSubject.Value, systemDarkModeSubject.Value));
        appUISubject = new BehaviorSubject<UIParameters>(
            UIParameters.Create(accentColorSubject.Value, appsDarkModeSubject.Value));

        accentColorSubject
            .CombineLatest(systemDarkModeSubject, (accentColor, isDarkMode) => new { accentColor, isDarkMode })
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => systemUISubject.OnNext(UIParameters.Create(x.accentColor, x.isDarkMode)))
            .DisposeWith(disposable);

        accentColorSubject
            .CombineLatest(appsDarkModeSubject, (accentColor, isDarkMode) => new { accentColor, isDarkMode })
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => appUISubject.OnNext(UIParameters.Create(x.accentColor, x.isDarkMode)))
            .DisposeWith(disposable);

        SystemDarkMode = systemDarkModeSubject.AsObservable();
        AppsDarkMode = appsDarkModeSubject.AsObservable();
        AccentColor = accentColorSubject.AsObservable();
        PrimaryScreen = primaryScreenSubject.AsObservable();
        Dpi = dpiSubject.AsObservable();

        SystemUI = systemUISubject.AsObservable();
        AppUI = appUISubject.AsObservable();

        AppShutdown = Observable
            .FromEventPattern<EventHandler, EventArgs>(h => Application.ApplicationExit += h, h => Application.ApplicationExit -= h)
            .Select(_ => true)
            .AsObservable();
        AppException = Observable
            .FromEventPattern<ThreadExceptionEventHandler, ThreadExceptionEventArgs>(h => Application.ThreadException += h, h => Application.ThreadException -= h)
            .Select(x => x.EventArgs.Exception)
            .AsObservable();

        messageQueue.Subscribe(WM_WININICHANGE, ProcessMessage).DisposeWith(disposable);
        messageQueue.Subscribe(WM_DISPLAYCHANGE, ProcessMessage).DisposeWith(disposable);
        messageQueue.Subscribe(WM_DPICHANGED, ProcessMessage).DisposeWith(disposable);
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public IObservable<bool> SystemDarkMode { get; }

    public IObservable<bool> AppsDarkMode { get; }

    public IObservable<Color> AccentColor { get; }

    public IObservable<Screen?> PrimaryScreen { get; }

    public IObservable<int> Dpi { get; }

    public IObservable<UIParameters> SystemUI { get; }

    public IObservable<UIParameters> AppUI { get; }

    public IObservable<bool> AppShutdown { get; }

    public IObservable<Exception> AppException { get; }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_WININICHANGE && Marshal.PtrToStringUni(lParam) == "ImmersiveColorSet")
        {
            systemDarkModeSubject.OnNext(IsSystemUseDarkMode());
            appsDarkModeSubject.OnNext(IsAppUseDarkMode());
            accentColorSubject.OnNext(GetAccentColor());
        }

        if (messageId == WM_DISPLAYCHANGE)
        {
            primaryScreenSubject.OnNext(Screen.PrimaryScreen);
        }

        if (messageId == WM_DPICHANGED)
        {
            dpiSubject.OnNext(HiWord(wParam));
        }
    }

    private bool IsAppUseDarkMode()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(PERSONALIZE_KEY, false))
        {
            return key?.GetValue(PERSONALIZE_APP_VALUE)?.ToString() != "1";
        }
    }

    private bool IsSystemUseDarkMode()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(PERSONALIZE_KEY, false))
        {
            return key?.GetValue(PERSONALIZE_SYSTEM_VALUE)?.ToString() != "1";
        }
    }
}