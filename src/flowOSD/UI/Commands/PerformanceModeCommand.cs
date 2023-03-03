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
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using flowOSD.Api;

public class PerformanceModeCommand : CommandBase
{
    private IAtk atk;

    public PerformanceModeCommand(IAtk atk)
    {
        this.atk = atk ?? throw new ArgumentNullException(nameof(atk));

        this.atk.PerformanceMode
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(performanceMode => IsChecked = performanceMode != PerformanceMode.Balanced)
            .DisposeWith(Disposable);

        Description = "Toggle Performance Mode";
        Enabled = true;
    }

    public override string Name => nameof(PerformanceModeCommand);

    public override bool CanExecuteWithHotKey => true;

    public override async void Execute(object parameter = null)
    {
        if (!Enabled)
        {
            return;
        }

        if (parameter is PerformanceMode performanceMode)
        {
            atk.SetPerformanceMode(performanceMode);
        }
        else
        {
            var nextPerformanceMode = await GetNextPerformanceMode();
            if (nextPerformanceMode != null)
            {
                atk.SetPerformanceMode(nextPerformanceMode.Value);
            }
        }
    }

    private async Task<PerformanceMode?> GetNextPerformanceMode()
    {
        var powerMode = await atk.PerformanceMode.FirstAsync();
        switch (powerMode)
        {
            case PerformanceMode.Silent:
                return PerformanceMode.Balanced;
            case PerformanceMode.Balanced:
                return PerformanceMode.Turbo;
            case PerformanceMode.Turbo:
                return PerformanceMode.Silent;

            default:
                return null;
        }
    }
}
