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
namespace flowOSD.UI.Commands;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using flowOSD.Api;
using flowOSD.Native;

sealed class SuspendCommand : CommandBase
{
    public const string HIBERNATE = "hibernate";
    public const string SLEEP = "sleep";

    private static readonly IList<ParameterInfo> parameters = new ReadOnlyCollection<ParameterInfo>(
        new ParameterInfo[]
        {
            new ParameterInfo(HIBERNATE, "Hibernate"),
            new ParameterInfo(SLEEP, "Sleep")
        });

    public SuspendCommand()
    {
        Text = "Suspend";
        Description = Text;
        Enabled = true;
    }

    public override string Name => nameof(SuspendCommand);

    public override bool CanExecuteWithHotKey => true;

    public override IList<ParameterInfo> Parameters => parameters;

    public override void Execute(object? parameter = null)
    {
        if (parameter is string mode == false || !(mode != HIBERNATE || mode != SLEEP))
        {
            return;
        }

        Powrprof.SetSuspendState(mode == HIBERNATE, true, true);
    }
}
