/*  Copyright © 2021, Albert Akhmetov <akhmetov@live.com>   
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
using static Extensions;

internal class GeneralConfigPage : TableLayoutPanel
{
    private CompositeDisposable disposable = new CompositeDisposable();

    public GeneralConfigPage(IConfig config)
    {
        Text = "General";

        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

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
            y.Text = "Run at logon";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.DataBindings.Add(
                "Checked",
                config.UserConfig,
                nameof(UserConfig.RunAtStartup),
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            y.DisposeWith(disposable);
        });

        this.Add<Label>(0, 1, y =>
        {
            y.AutoSize = true;
            y.Margin = labelMargin;
            y.Text = "Indicates whether the app starts when a user is logged on.";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.ForeColor = SystemColors.ControlDarkDark;

            y.DisposeWith(disposable);
        });

        this.Add<CheckBox>(0, 2, y =>
        {
            y.AutoSize = true;
            y.Margin = new Padding(20, 5, 0, 5);
            y.Text = "Disable TouchPad in tablet mode";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.DataBindings.Add(
                "Checked",
                config.UserConfig,
                nameof(UserConfig.DisableTouchPadInTabletMode),
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            y.DisposeWith(disposable);
        });

        this.Add<Label>(0, 3, y =>
        {
            y.AutoSize = true;
            y.Margin = new Padding(15, 5, 0, 15);
            y.Text = "Indicates whether TouchPad is disabled when the notebook goes into the tablet mode.";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.ForeColor = SystemColors.ControlDarkDark;

            y.DisposeWith(disposable);
        });

        this.Add<CheckBox>(0, 4, y =>
        {
            y.AutoSize = true;
            y.Margin = new Padding(20, 5, 0, 5);
            y.Text = "Control display refresh rate";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.DataBindings.Add(
                "Checked",
                config.UserConfig,
                nameof(UserConfig.ControlDisplayRefreshRate),
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            y.DisposeWith(disposable);
        });

        this.Add<Label>(0, 5, y =>
        {
            y.AutoSize = true;
            y.Margin = new Padding(15, 5, 0, 15);
            y.Text = "Indicates whether display refresh rate is dependent on the power source.";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.ForeColor = SystemColors.ControlDarkDark;

            y.DisposeWith(disposable);
        });

        this.Add<CheckBox>(0, 6, y =>
        {
            y.AutoSize = true;
            y.Margin = new Padding(20, 5, 0, 5);
            y.Text = "Use ROG key as Print Screen";
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.DataBindings.Add(
                "Checked",
                config.UserConfig,
                nameof(UserConfig.UseRogKey),
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            y.DisposeWith(disposable);
        });

        this.Add<Label>(0, 7, y =>
        {
            y.AutoSize = true;
            y.Margin = new Padding(15, 5, 0, 15);
            y.Text = "Disable this option if ASUS Armoury Crate is used.";
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
