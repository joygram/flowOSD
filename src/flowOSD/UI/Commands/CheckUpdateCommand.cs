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

namespace flowOSD.UI.Commands;

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using flowOSD.Api;

sealed class CheckUpdateCommand : CommandBase
{
    private IUpdater updater;

    public CheckUpdateCommand(IUpdater updater)
    {
        this.updater = updater ?? throw new ArgumentNullException(nameof(updater));

        Text = "Check for updates";
        Enabled = true;
    }

    public override string Name => nameof(CheckUpdateCommand);

    public override bool CanExecuteWithHotKey => false;

    public override async void Execute(object? parameter = null)
    {
        Enabled = false;
        await updater.CheckUpdate(true);
        Enabled = true;
    }
}
