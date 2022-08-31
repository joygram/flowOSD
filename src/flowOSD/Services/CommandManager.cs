﻿/*  Copyright © 2021-2022, Albert Akhmetov <akhmetov@live.com>   
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

    public CommandManager()
    {
    }

    public void Register(ICommand command, params ICommand[] commands)
    {
        names[command.Name] = command;

        foreach(var c in commands)
        {
            names[c.Name] = c;
        }
    }

    public ICommand Resolve(string commandName)
    {
        return !string.IsNullOrEmpty(commandName) && names.TryGetValue(commandName, out ICommand command) ? command : null;
    }

    public IList<ICommand> Commands => names.Values.Where(i => i.CanExecuteWithHotKey).ToArray();
}