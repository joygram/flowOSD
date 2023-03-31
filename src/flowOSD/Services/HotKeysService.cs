﻿/*  Copyright © 2021-2023, Albert Akhmetov <akhmetov@live.com>   
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

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows.Input;
using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;

sealed class HotKeysService : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private ICommandService commandService;
    private IKeyboard keyboard;

    private Dictionary<AtkKey, Binding> keys = new Dictionary<AtkKey, Binding>();

    public HotKeysService(IConfig config, ICommandService commandService, IKeyboard keyboard)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        this.keyboard = keyboard ?? throw new ArgumentNullException(nameof(keyboard));

        this.config.HotKeys.KeyChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateBindings)
            .DisposeWith(disposable);

        this.keyboard.KeyPressed
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ExecuteCommand)
            .DisposeWith(disposable);

        RegisterHotKeys();
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void Register(AtkKey key, HotKeysConfig.Command? commandInfo)
    {
        var command = commandService.Resolve(commandInfo?.Name);

        if (command != null)
        {
            keys[key] = new Binding(command, commandInfo?.Parameter);
        }
        else
        {
            keys.Remove(key);
        }
    }

    private void RegisterHotKeys()
    {
        foreach (var key in Enum.GetValues<AtkKey>())
        {
            UpdateBindings(key);
        }
    }

    private void UpdateBindings(AtkKey key)
    {
        Register(key, config.HotKeys[key]);
    }

    private void ExecuteCommand(AtkKey key)
    {
        if (keys.TryGetValue(key, out Binding? binding))
        {
            binding.Execute();
        }
    }

    private sealed class Binding
    {
        public Binding(ICommand command, object? commandParameter = null)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            CommandParameter = commandParameter;
        }

        public ICommand Command { get; }

        public object? CommandParameter { get; }

        public void Execute()
        {
            Command.Execute(CommandParameter);
        }
    }
}
