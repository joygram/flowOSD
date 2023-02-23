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

sealed class ToggleGpuCommand : CommandBase
{
    private IGpu gpu;

    public ToggleGpuCommand(IGpu gpu)
    {
        this.gpu = gpu;

        this.gpu.IsEnabled
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Text = x ? "Disable eGPU" : "Enable eGPU")
            .DisposeWith(Disposable);

        Description = "Toggle eGPU";
        Enabled = true;
    }

    public override string Name => nameof(ToggleGpuCommand);

    public async override void Execute(object parameter = null)
    {
        if (! await Confirm())
        {
            return;
        }

        try
        {
            gpu.Toggle();
        }
        catch (Exception ex)
        {
            Extensions.TraceException(ex, "Error is occurred while toggling GPU (UI).");
        }
    }

    private async Task<bool> Confirm()
    {
        var isEnabled = await gpu.IsEnabled.FirstAsync();

        return DialogResult.Yes == MessageBox.Show(
            isEnabled ? "Do you want to turn off eGPU?" : "Do you want to turn on eGPU?",
            "External GPU",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
    }
}
