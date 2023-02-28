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
using static Native;

sealed partial class SystemEvents : ISystemEvents, IDisposable
{
    private const int SM_CONVERTIBLESLATEMODE = 0x2003;


    private CompositeDisposable disposable = new CompositeDisposable();

    private NativeUI nativeUI;

    private BehaviorSubject<bool> systemDarkModeSubject;
    private BehaviorSubject<bool> appsDarkModeSubject;
    private BehaviorSubject<Color> accentColorSubject;
    private BehaviorSubject<bool> tabletModeSubject;
    private BehaviorSubject<Screen> primaryScreenSubject;
    private BehaviorSubject<int> dpiSubject;
    private BehaviorSubject<UIParameters> systemUISubject, appUISubject;

    public SystemEvents(IMessageQueue messageQueue)
    {
        nativeUI = new NativeUI(messageQueue).DisposeWith(disposable);

        systemDarkModeSubject = new BehaviorSubject<bool>(ShouldSystemUseDarkMode());
        appsDarkModeSubject = new BehaviorSubject<bool>(ShouldAppsUseDarkMode());
        accentColorSubject = new BehaviorSubject<Color>(GetAccentColor());
        tabletModeSubject = new BehaviorSubject<bool>(GetSystemMetrics(SM_CONVERTIBLESLATEMODE) == 0);

        primaryScreenSubject = new BehaviorSubject<Screen>(Screen.PrimaryScreen);
        dpiSubject = new BehaviorSubject<int>(GetDpiForWindow(nativeUI.Handle));

        systemUISubject = new BehaviorSubject<UIParameters>(
            UIParameters.Create(accentColorSubject.Value, systemDarkModeSubject.Value));
        appUISubject = new BehaviorSubject<UIParameters>(
            UIParameters.Create(accentColorSubject.Value, appsDarkModeSubject.Value));

        accentColorSubject
            .CombineLatest(systemDarkModeSubject, (accentColor, isDarkMode) => new { accentColor, isDarkMode })
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => systemUISubject.OnNext(UIParameters.Create(x.accentColor, x.isDarkMode)))
            .DisposeWith(disposable);

        accentColorSubject
            .CombineLatest(appsDarkModeSubject, (accentColor, isDarkMode) => new { accentColor, isDarkMode })
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => appUISubject.OnNext(UIParameters.Create(x.accentColor, x.isDarkMode)))
            .DisposeWith(disposable);

        SystemDarkMode = systemDarkModeSubject.AsObservable();
        AppsDarkMode = appsDarkModeSubject.AsObservable();
        AccentColor = accentColorSubject.AsObservable();
        TabletMode = tabletModeSubject.AsObservable();
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

    public IObservable<bool> TabletMode { get; }

    public IObservable<Screen> PrimaryScreen { get; }

    public IObservable<int> Dpi { get; }

    public IObservable<UIParameters> SystemUI { get; }

    public IObservable<UIParameters> AppUI { get; }

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

        if (messageId == WM_DPICHANGED)
        {
            dpiSubject.OnNext((int)HiWord(wParam));
        }
    }

    private sealed class NativeUI : NativeWindow, IDisposable
    {
        private IMessageQueue messageQueue;

        private Form form;

        public NativeUI(IMessageQueue messageQueue)
        {
            form = new Form();
            var handle = form.Handle;

            this.messageQueue = messageQueue;

            AssignHandle(handle);
        }

        ~NativeUI()
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
            form.Dispose();
            form = null;

            ReleaseHandle();
        }

        protected override void WndProc(ref Message message)
        {
            messageQueue.Push(ref message);

            base.WndProc(ref message);
        }
    }
}