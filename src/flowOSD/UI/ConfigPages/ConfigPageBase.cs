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
using flowOSD.Extensions;
using System.Reactive.Disposables;

internal class ConfigPageBase : TableLayoutPanel
{
    protected static readonly Padding CheckBoxMargin = new Padding(20, 5, 0, 5);
    protected static readonly Padding LabelMargin = new Padding(15, 5, 0, 15);

    protected ConfigPageBase(IConfig config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));

        Dock = DockStyle.Top;
        AutoScroll = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
    }

    protected CompositeDisposable Disposable { get; private set; } = new CompositeDisposable();

    protected IConfig Config { get; }

    protected void AddConfig(string text, string description, string propertyName)
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
                Config.UserConfig,
                propertyName,
                false,
                DataSourceUpdateMode.OnPropertyChanged);

            y.DisposeWith(Disposable);
        });

        RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
        this.Add<Label>(0, RowStyles.Count - 1, y =>
        {
            y.AutoSize = true;
            y.Margin = LabelMargin;
            y.Text = description;
            y.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            y.ForeColor = SystemColors.ControlDarkDark;

            y.DisposeWith(Disposable);
        });
    }

    protected override void Dispose(bool disposing)
    {
        Disposable?.Dispose();
        Disposable = null;

        base.Dispose(disposing);
    }
}
