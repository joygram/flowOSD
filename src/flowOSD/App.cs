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
namespace flowOSD;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using flowOSD.Api;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;
using flowOSD.Hardware;
using flowOSD.Services;
using flowOSD.UI;
using flowOSD.UI.Commands;
using flowOSD.UI.Components;
using static flowOSD.Extensions.Common;

sealed partial class App : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;

    private MessageQueue messageQueue;
    private SystemEvents systemEvents;
    private KeysSender keysSender;

    private Osd osd;
    private MainUI mainUI;

    private HardwareService hardwareService;
    private CommandManager commandManager;

    public App(IConfig config)
    {
        this.config = config;


        ApplicationContext = new ApplicationContext().DisposeWith(disposable);

        messageQueue = new MessageQueue().DisposeWith(disposable);
        hardwareService = new HardwareService(config, messageQueue).DisposeWith(disposable);

        systemEvents = new SystemEvents(messageQueue).DisposeWith(disposable);
        systemEvents.AppException
            .Subscribe(ex =>
            {
                TraceException(ex, "Unhandled application exception");
                MessageBox.Show(ex.Message, "Unhandled application exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            })
            .DisposeWith(disposable);
        keysSender = new KeysSender();

        // Notifications

        osd = new Osd(systemEvents);
        new Notifications(config, osd, hardwareService).DisposeWith(disposable);

        // Commands

        commandManager = new CommandManager();
        commandManager.Register(
            new DisplayRefreshRateCommand(hardwareService.ResolveNotNull<IPowerManagement>(), hardwareService.ResolveNotNull<IDisplay>(), config.UserConfig),
            new ToggleTouchPadCommand(hardwareService.ResolveNotNull<ITouchPad>()),
            new ToggleBoostCommand(hardwareService.ResolveNotNull<IPowerManagement>()),
            new ToggleGpuCommand(hardwareService.ResolveNotNull<IAtk>(), config),
            new PerformanceModeCommand(hardwareService.ResolveNotNull<IAtk>()),
            new PowerModeCommand(hardwareService.ResolveNotNull<IPowerManagement>()),
            new SettingsCommand(config, commandManager),
            new AboutCommand(config),
            new ExitCommand(),
            new PrintScreenCommand(keysSender),
            new ClipboardCopyPlainTextCommand(keysSender),
            new ClipboardPastePlainTextCommand(keysSender)
        );

        mainUI = new MainUI(
            config,
            systemEvents,
            commandManager,
            hardwareService).DisposeWith(disposable);

        commandManager.Register(new MainUICommand(mainUI));

        new NotifyIconUI(
            config,
            messageQueue,
            systemEvents,
            commandManager,
            hardwareService.ResolveNotNull<IAtkWmi>()).DisposeWith(disposable);

        new HotKeyManager(
            config,
            commandManager,
            hardwareService.ResolveNotNull<IKeyboard>()).DisposeWith(disposable);
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }



    public ApplicationContext ApplicationContext { get; }
}