/*  Copyright © 2021-2022, Albert Akhmetov <akhmetov@live.com>   
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
using static Native;

sealed partial class SystemEvents : ISystemEvents, IDisposable
{
    private const int SM_CONVERTIBLESLATEMODE = 0x2003;
    private const int WM_WININICHANGE = 0x001A;
    private const int WM_DISPLAYCHANGE = 0x7E;

    private CompositeDisposable disposable = new CompositeDisposable();

    private BehaviorSubject<bool> systemDarkModeSubject;
    private BehaviorSubject<bool> appsDarkModeSubject;
    private BehaviorSubject<Color> accentColorSubject;
    private BehaviorSubject<bool> tabletModeSubject;
    private BehaviorSubject<Screen> primaryScreenSubject;

    public SystemEvents(IMessageQueue messageQueue)
    {
        systemDarkModeSubject = new BehaviorSubject<bool>(ShouldSystemUseDarkMode());
        appsDarkModeSubject = new BehaviorSubject<bool>(ShouldAppsUseDarkMode());
        accentColorSubject = new BehaviorSubject<Color>(GetAccentColor());
        tabletModeSubject = new BehaviorSubject<bool>(GetSystemMetrics(SM_CONVERTIBLESLATEMODE) == 0);

        primaryScreenSubject = new BehaviorSubject<Screen>(Screen.PrimaryScreen);

        SystemDarkMode = systemDarkModeSubject.AsObservable();
        AppsDarkMode = appsDarkModeSubject.AsObservable();
        AccentColor = accentColorSubject.AsObservable();
        TabletMode = tabletModeSubject.AsObservable();
        PrimaryScreen = primaryScreenSubject.AsObservable();

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
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public IObservable<bool> SystemDarkMode { get; }

    public IObservable<bool> AppsDarkMode { get; }

    public IObservable<Color> AccentColor { get; }

    public IObservable<bool> TabletMode { get; }

    public IObservable<Screen> PrimaryScreen { get; }

    public IObservable<bool> AppShutdown { get; }

    public IObservable<Exception> AppException { get; }

    private void ProcessMessage(int messageId, IntPtr wParam, IntPtr lParam)
    {
        if (messageId == WM_WININICHANGE && Marshal.PtrToStringUni(lParam) == "ImmersiveColorSet")
        {
            systemDarkModeSubject.OnNext(ShouldSystemUseDarkMode());
            appsDarkModeSubject.OnNext(ShouldAppsUseDarkMode());
            accentColorSubject.OnNext(GetAccentColor());
        }

        if (messageId == WM_WININICHANGE && Marshal.PtrToStringUni(lParam) == "ConvertibleSlateMode")
        {
            tabletModeSubject.OnNext(GetSystemMetrics(SM_CONVERTIBLESLATEMODE) == 0);
        }

        if (messageId == WM_DISPLAYCHANGE)
        {
            primaryScreenSubject.OnNext(Screen.PrimaryScreen);
        }
    }
}