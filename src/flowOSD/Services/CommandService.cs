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

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.Api.Hardware;
using flowOSD.UI.Commands;

sealed class CommandService : ICommandService
{
    private IConfig config;
    private IHardwareService hardwareService;
    private IKeysSender keysSender;
    private IUpdater updater;

    private Dictionary<string, CommandBase> names = new Dictionary<string, CommandBase>();

    public CommandService(
        IConfig config, 
        IHardwareService hardwareService, 
        IKeysSender keysSender, 
        ISystemEvents systemEvents, 
        IUpdater updater,
        IOsd osd)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.hardwareService = hardwareService ?? throw new ArgumentNullException(nameof(hardwareService));
        this.keysSender = keysSender ?? throw new ArgumentNullException(nameof(keysSender));
        this.updater = updater ?? throw new ArgumentNullException(nameof(updater));

        Register(
            new DisplayRefreshRateCommand(
                hardwareService.ResolveNotNull<IPowerManagement>(),
                hardwareService.ResolveNotNull<IDisplay>(),
                config.Common),
            new DisplayBrightnessCommand(config, osd, hardwareService.ResolveNotNull<IDisplayBrightness>()),
            new KeyboardBacklightCommand(config, osd, hardwareService.ResolveNotNull<IKeyboardBacklight>()),
            new TouchPadCommand(hardwareService.ResolveNotNull<ITouchPad>()),
            new MicrophoneCommand(config, osd, hardwareService.ResolveNotNull<IMicrophone>()),
            new ToggleBoostCommand(hardwareService.ResolveNotNull<IPowerManagement>()),
            new GpuCommand(hardwareService.ResolveNotNull<IAtk>(), config),
            new PerformanceModeCommand(hardwareService.ResolveNotNull<IAtk>()),
            new PowerModeCommand(hardwareService.ResolveNotNull<IPowerManagement>()),
            new SuspendCommand(),
            new SettingsCommand(config, this, systemEvents, hardwareService),
            new ExitCommand(),
            new PrintScreenCommand(keysSender),
            new ClipboardCopyPlainTextCommand(keysSender),
            new ClipboardPastePlainTextCommand(keysSender));
    }

    public void Register(CommandBase command, params CommandBase[] commands)
    {
        names[command.Name] = command;

        foreach (var c in commands)
        {
            names[c.Name] = c;
        }
    }

    public CommandBase? Resolve(string? commandName)
    {
        return !string.IsNullOrEmpty(commandName) && names.TryGetValue(commandName, out CommandBase? command) ? command : null;
    }

    public T? Resolve<T>() where T : CommandBase
    {
        return Resolve(typeof(T).Name) as T;
    }

    public T ResolveNotNull<T>() where T : CommandBase
    {
        return Resolve<T>() ?? throw new InvalidOperationException($"Can't resolve {typeof(T).Name}");
    }

    public bool TryResolve<T>(out T? command) where T : CommandBase
    {
        command = Resolve<T>();
        return command != null;
    }

    public IList<CommandBase> Commands => names.Values.Where(i => i.CanExecuteWithHotKey).ToArray();
}