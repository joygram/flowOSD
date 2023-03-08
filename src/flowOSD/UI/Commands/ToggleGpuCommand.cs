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

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using flowOSD.Api;

sealed class ToggleGpuCommand : CommandBase
{
    private IAtk atk;
    private IConfig config;

    public ToggleGpuCommand(IAtk atk, IConfig config)
    {
        this.atk = atk ?? throw new ArgumentNullException(nameof(atk));
        this.config = config ?? throw new ArgumentNullException(nameof(config));

        this.atk.GpuMode
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(Update)
            .DisposeWith(Disposable);

        Description = "Toggle dGPU";
        Enabled = true;
    }

    public override string Name => nameof(ToggleGpuCommand);

    public async override void Execute(object parameter = null)
    {
        var isGpuEnabled = await atk.GpuMode.FirstAsync() == GpuMode.dGpu;
        if (!Confirm(isGpuEnabled))
        {
            return;
        }

        try
        {
            atk.SetGpuMode(isGpuEnabled ? GpuMode.iGpu : GpuMode.dGpu);
        }
        catch (Exception ex)
        {
            Extensions.TraceException(ex, "Error is occurred while toggling GPU (UI).");
        }
    }

    private bool Confirm(bool isGpuEnabled)
    {
        return !config.UserConfig.ConfirmGpuModeChange || DialogResult.Yes == MessageBox.Show(
            isGpuEnabled ? "Do you want to turn off dGPU?" : "Do you want to turn on dGPU?",
            "Discrete GPU",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
    }

    private void Update(GpuMode gpuMode)
    {
        IsChecked = gpuMode == GpuMode.dGpu;
        Text = IsChecked ? "Disable dGPU" : "Enable dGPU";
    }
}
