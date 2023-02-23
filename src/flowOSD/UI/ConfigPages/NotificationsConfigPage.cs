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
    private CompositeDisposable disposable = new CompositeDisposable();

    public NotificationsConfigPage(IConfig config)
    {
        Text = "Notifications";

        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));

        var checkBoxMargin = new Padding(20, 5, 0, 5);
        var labelMargin = new Padding(15, 5, 0, 15);

        this.Add<CheckBox>(0, 0, y =>
        {
            y.AutoSize = true;
            y.Margin = checkBoxMargin;
            y.Text = "Show power source notifications";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.DataBindings.Add(
                "Checked",
                config.UserConfig,
                nameof(UserConfig.ShowPowerSourceNotification),
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            y.DisposeWith(disposable);
        });

        this.Add<Label>(0, 1, y =>
        {
            y.AutoSize = true;
            y.Margin = labelMargin;
            y.Text = "Indicates whether notification shows when notebook power source changes.";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.ForeColor = SystemColors.ControlDarkDark;

            y.DisposeWith(disposable);
        });

        this.Add<CheckBox>(0, 2, y =>
        {
            y.AutoSize = true;
            y.Margin = checkBoxMargin;
            y.Text = "Show CPU boost mode notifications";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.DataBindings.Add(
                "Checked",
                config.UserConfig,
                nameof(UserConfig.ShowBoostNotification),
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            y.DisposeWith(disposable);
        });

        this.Add<Label>(0, 3, y =>
        {
            y.AutoSize = true;
            y.Margin = labelMargin;
            y.Text = "Indicates whether notification shows when CPU boost mode is disabled or enabled.";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.ForeColor = SystemColors.ControlDarkDark;

            y.DisposeWith(disposable);
        });

        this.Add<CheckBox>(0, 4, y =>
        {
            y.AutoSize = true;
            y.Margin = checkBoxMargin;
            y.Text = "Show TouchPad notifications";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.DataBindings.Add(
                "Checked",
                config.UserConfig,
                nameof(UserConfig.ShowTouchPadNotification),
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            y.DisposeWith(disposable);
        });

        this.Add<Label>(0, 5, y =>
        {
            y.AutoSize = true;
            y.Margin = labelMargin;
            y.Text = "Indicates whether notification shows when TochPad is disabled or enabled.";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.ForeColor = SystemColors.ControlDarkDark;

            y.DisposeWith(disposable);
        });

        this.Add<CheckBox>(0, 6, y =>
        {
            y.AutoSize = true;
            y.Margin = checkBoxMargin;
            y.Text = "Show display refesh rate notifications";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.DataBindings.Add(
                "Checked",
                config.UserConfig,
                nameof(UserConfig.ShowDisplayRateNotification),
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            y.DisposeWith(disposable);
        });

        this.Add<Label>(0, 7, y =>
        {
            y.AutoSize = true;
            y.Margin = labelMargin;
            y.Text = "Indicates whether notification shows when display refresh rate changes.";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.ForeColor = SystemColors.ControlDarkDark;

            y.DisposeWith(disposable);
        });

        this.Add<CheckBox>(0, 8, y =>
        {
            y.AutoSize = true;
            y.Margin = checkBoxMargin;
            y.Text = "Show microphone status notifications";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.DataBindings.Add(
                "Checked",
                config.UserConfig,
                nameof(UserConfig.ShowMicNotification),
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            y.DisposeWith(disposable);
        });

        this.Add<Label>(0, 9, y =>
        {
            y.AutoSize = true;
            y.Margin = labelMargin;
            y.Text = "Indicates whether notification shows when microphone state changes.";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.ForeColor = SystemColors.ControlDarkDark;

            y.DisposeWith(disposable);
        });
    }

    protected override void Dispose(bool disposing)
    {
        disposable?.Dispose();
        disposable = null;

        base.Dispose(disposing);
    }
}
