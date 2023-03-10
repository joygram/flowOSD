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

sealed class DisplayRefreshRateCommand : CommandBase
{
    private IPowerManagement powerManagement;
    private IDisplay display;
    private UserConfig userConfig;

    public DisplayRefreshRateCommand(IPowerManagement powerManagement, IDisplay display, UserConfig userConfig)
    {
        this.powerManagement = powerManagement ?? throw new ArgumentNullException(nameof(powerManagement));
        this.display = display ?? throw new ArgumentNullException(nameof(display));
        this.userConfig = userConfig ?? throw new ArgumentNullException(nameof(userConfig));

        display.RefreshRates
            .CombineLatest(display.IsEnabled, (x, isEnabled) => isEnabled && x.IsLowAvailable && x.IsHighAvailable)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => Enabled = x)
            .DisposeWith(Disposable);

        display.RefreshRate
            .Throttle(TimeSpan.FromMilliseconds(200))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(Update)
            .DisposeWith(Disposable);

        Description = "Toggle High Refresh Rate";
        Enabled = true;
    }

    public override string Name => nameof(DisplayRefreshRateCommand);

    public override async void Execute(object parameter = null)
    {
        try
        {
            if (parameter is uint refreshRate == false)
            {
                refreshRate = await display.RefreshRate.FirstAsync();
            }

            if (refreshRate == 0)
            {
                return;
            }

            var refreshRates = await display.RefreshRates.FirstAsync();
            var newRefreshRate = DisplayRefreshRates.IsHigh(refreshRate) ? refreshRates.Low : refreshRates.High;
            if (newRefreshRate.HasValue)
            {
                if (!display.SetRefreshRate(newRefreshRate.Value))
                {
                    return;
                }

                if (await powerManagement.IsDC.FirstAsync())
                {
                    userConfig.HighDisplayRefreshRateDC = !DisplayRefreshRates.IsHigh(newRefreshRate.Value);
                }
                else
                {
                    userConfig.HighDisplayRefreshRateAC = !DisplayRefreshRates.IsHigh(newRefreshRate.Value);
                }
            }
        }
        catch (Exception ex)
        {
            Extensions.TraceException(ex, "Error is occurred while toggling display refresh rate (UI).");
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Update(uint refreshRate)
    {
        IsChecked = DisplayRefreshRates.IsHigh(refreshRate);
        Text = IsChecked ? "Disable High Refresh Rate" : "Enable High Refresh Rate";
    }
}
