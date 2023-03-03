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
namespace flowOSD.UI.ConfigPages;

using flowOSD.Api;
using System.Reactive.Disposables;

internal class NotificationsConfigPage : TableLayoutPanel
{
    private readonly Padding CheckBoxMargin = new Padding(20, 5, 0, 5);
    private readonly Padding LabelMargin = new Padding(15, 5, 0, 15);

    private CompositeDisposable disposable = new CompositeDisposable();
    private IConfig config;

    public NotificationsConfigPage(IConfig config)
    {
        Dock = DockStyle.Top;
        AutoScroll = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        this.config = config;

        Text = "Notifications";
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddConfig(
            "Show power source notifications",
            "Indicates whether notification shows when notebook power source changes.",
            nameof(UserConfig.ShowPowerSourceNotification));
        AddConfig(
            "Show CPU boost mode notifications",
            "Indicates whether notification shows when CPU boost mode is disabled or enabled.",
            nameof(UserConfig.ShowBoostNotification));
        AddConfig(
            "Show performance mode notifications",
            "Indicates whether notification shows when performance override mode changes.",
            nameof(UserConfig.ShowPerformanceModeNotification));
        AddConfig(
            "Show power mode notifications",
            "Indicates whether notification shows when power mode changes.",
            nameof(UserConfig.ShowPowerModeNotification));
        AddConfig(
            "Show TouchPad notifications",
            "Indicates whether notification shows when TochPad is disabled or enabled.",
            nameof(UserConfig.ShowTouchPadNotification));
        AddConfig(
            "Show display refesh rate notifications",
            "Indicates whether notification shows when display refresh rate changes.",
            nameof(UserConfig.ShowDisplayRateNotification));
        AddConfig(
            "Show microphone status notifications",
            "Indicates whether notification shows when microphone state changes.",
            nameof(UserConfig.ShowMicNotification));
        AddConfig(
            "Show eGPU notifications",
            "Indicates whether notification shows when eGPU is disabled or enabled.",
            nameof(UserConfig.ShowGpuNotification));
    }

    protected override void Dispose(bool disposing)
    {
        disposable?.Dispose();
        disposable = null;

        base.Dispose(disposing);
    }

    private void AddConfig(string text, string description, string propertyName)
    {
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        this.Add<CheckBox>(0, RowStyles.Count - 1, y =>
        {
            y.AutoSize = true;
            y.Margin = CheckBoxMargin;
            y.Text = text;
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.DataBindings.Add(
                "Checked",
                config.UserConfig,
                propertyName,
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            y.DisposeWith(disposable);
        });

        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        this.Add<Label>(0, RowStyles.Count - 1, y =>
        {
            y.AutoSize = true;
            y.Margin = LabelMargin;
            y.Text = description;
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.ForeColor = SystemColors.ControlDarkDark;

            y.DisposeWith(disposable);
        });
    }
}
