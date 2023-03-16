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

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows.Input;
using flowOSD.Api;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;

sealed class HotKeyManager : IDisposable
{
    private CompositeDisposable? disposable = new CompositeDisposable();

    private IConfig config;
    private ICommandManager commandManager;

    private Dictionary<AtkKey, Binding> keys = new Dictionary<AtkKey, Binding>();

    public HotKeyManager(IConfig config, ICommandManager commandManager, IKeyboard keyboard)
    {
        this.config = config;
        this.commandManager = commandManager;

        this.config.UserConfig.PropertyChanged
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(UpdateBindings)
            .DisposeWith(disposable);

        keyboard.KeyPressed
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(ExecuteCommand)
            .DisposeWith(disposable);
    }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void Register(AtkKey key, string? commandName, object? commandParameter = null)
    {
        var command = commandManager.Resolve(commandName);

        if (command != null)
        {
            keys[key] = new Binding(command, commandParameter);
        }
        else
        {
            keys.Remove(key);
        }
    }

    private void RegisterHotKeys()
    {
        Register(AtkKey.Aura, config.UserConfig.AuraCommand);
        Register(AtkKey.Fan, config.UserConfig.FanCommand);
        Register(AtkKey.Rog, config.UserConfig.RogCommand);
        Register(AtkKey.Copy, config.UserConfig.CopyCommand);
        Register(AtkKey.Paste, config.UserConfig.PasteCommand);
    }

    private void UpdateBindings(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(UserConfig.AuraCommand):
                Register(AtkKey.Aura, config.UserConfig.AuraCommand);
                break;

            case nameof(UserConfig.FanCommand):
                Register(AtkKey.Fan, config.UserConfig.FanCommand);
                break;

            case nameof(UserConfig.RogCommand):
                Register(AtkKey.Rog, config.UserConfig.RogCommand);
                break;

            case nameof(UserConfig.CopyCommand):
                Register(AtkKey.Copy, config.UserConfig.CopyCommand);
                break;

            case nameof(UserConfig.PasteCommand):
                Register(AtkKey.Paste, config.UserConfig.PasteCommand);
                break;

            case "":
            case null:
                RegisterHotKeys();
                break;
        }
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
