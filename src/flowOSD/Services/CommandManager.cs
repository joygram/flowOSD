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

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Api;

sealed class CommandManager : ICommandManager
{
    private Dictionary<string, ICommand> names = new Dictionary<string, ICommand>();
    private Dictionary<AtkKey, ICommand> keys = new Dictionary<AtkKey, ICommand>();

    public CommandManager(params ICommand[] commands)
    {
        foreach (var command in commands)
        {
            names[command.Name] = command;
        }
    }

    public void Register(AtkKey key, string commandName)
    {
        if (!string.IsNullOrEmpty(commandName) && names.TryGetValue(commandName, out ICommand nextCommand))
        {
            keys[key] = nextCommand;
        }
        else
        {
            keys.Remove(key);
        }
    }

    public ICommand Resolve(string commandName)
    {
        return !string.IsNullOrEmpty(commandName) && names.TryGetValue(commandName, out ICommand command) ? command : null;
    }

    public ICommand Resolve(AtkKey key)
    {
        return keys.TryGetValue(key, out ICommand command) ? command : null;
    }
}