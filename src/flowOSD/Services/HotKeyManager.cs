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

sealed class HotKeyManager : IHotKeyManager
{
    private ICommandManager commandManager;
    private Dictionary<AtkKey, Binding> keys = new Dictionary<AtkKey, Binding>();

    public HotKeyManager(ICommandManager commandManager)
    {
        this.commandManager = commandManager;
    }

    public void Register(AtkKey key, string commandName, object commandParameter = null)
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

    public void ExecuteCommand(AtkKey key)
    {
        if (keys.TryGetValue(key, out Binding binding))
        {
            binding.Execute();
        }
    }

    private sealed class Binding
    {
        public Binding(ICommand command, object commandParameter = null)
        {
            Command = command ?? throw new ArgumentNullException(nameof(command));
            CommandParameter = commandParameter;
        }

        public ICommand Command { get; }

        public object CommandParameter { get; }

        public void Execute()
        {
            Command.Execute(CommandParameter);
        }
    }
}
