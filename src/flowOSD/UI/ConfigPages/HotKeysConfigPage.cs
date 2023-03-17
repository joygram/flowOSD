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
using flowOSD.UI.Commands;
using flowOSD.UI.Components;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;

internal class HotKeysConfigPage : ConfigPageBase
{
    private CompositeDisposable disposable = new CompositeDisposable();
    private ICommandService commandService;

    public HotKeysConfigPage(IConfig config, CxTabListener tabListener, ICommandService commandService)
        : base(config, tabListener)
    {
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        Dock = DockStyle.Top;
        AutoScroll = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        Text = "HotKeys";

        this.Add<CxGrid>(0, 0, grid =>
        {
            RegisterCxItem(grid);

            grid.MouseClick += OnMouseClick;
            grid.Padding = new Padding(20, 20, 20, 10);
            grid.Dock = DockStyle.Top;
            grid.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grid.AutoSize = true;

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            Add(grid, "AURA", nameof(config.UserConfig.AuraCommand), value => config.UserConfig.AuraCommand = value);
            Add(grid, "FAN", nameof(config.UserConfig.FanCommand), value => config.UserConfig.FanCommand = value);
            Add(grid, "ROG", nameof(config.UserConfig.RogCommand), value => config.UserConfig.RogCommand = value);
            Add(grid, "Fn + C", nameof(config.UserConfig.CopyCommand), value => config.UserConfig.CopyCommand = value);
            Add(grid, "Fn + V", nameof(config.UserConfig.PasteCommand), value => config.UserConfig.PasteCommand = value);

            grid.Add<FlowLayoutPanel>(0, 5, 2, 1, x =>
            {
                x.MouseClick += OnMouseClick;
                x.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                x.AutoSize = true;
                x.Margin = new Padding(0, 0, 0, 10);
                x.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                x.AutoSize = true;

                x.Add<CxButton>(y =>
                {
                    y.Padding = new Padding(20, 5, 20, 5);

                    y.Text = "Disable all";
                    y.AutoSize = true;
                    y.Margin = new Padding(5, 0, 0, 0);
                    y.Click += (sender, e) =>
                    {
                        config.UserConfig.AuraCommand = null;
                        config.UserConfig.FanCommand = null;
                        config.UserConfig.RogCommand = null;
                        config.UserConfig.CopyCommand = null;
                        config.UserConfig.PasteCommand = null;
                    };

                    y.DisposeWith(disposable);
                    RegisterCxItem(y);
                });

                x.Add<CxButton>(y =>
                {
                    y.Padding = new Padding(20, 5, 20, 5);

                    y.Text = "Reset";
                    y.AutoSize = true;
                    y.Margin = new Padding(5, 0, 0, 0);
                    y.Click += (sender, e) =>
                    {
                        config.UserConfig.AuraCommand = nameof(DisplayRefreshRateCommand);
                        config.UserConfig.FanCommand = nameof(ToggleBoostCommand);
                        config.UserConfig.RogCommand = nameof(PrintScreenCommand);
                        config.UserConfig.CopyCommand = null;
                        config.UserConfig.PasteCommand = null;
                    };

                    y.DisposeWith(disposable);
                    RegisterCxItem(y);
                });
            });
        });
    }

    private void Add(CxGrid grid, string text, string propertyName, Action<string?> setValue)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Add<CxLabel>(0, grid.RowCount - 1, y =>
        {
            y.AutoSize = true;
            y.Margin = new Padding(5, 20, 20, 20);
            y.Text = text;
            y.Anchor = AnchorStyles.Left;
            y.ForeColor = SystemColors.ControlText;
            y.UseClearType = true;

            y.DisposeWith(disposable);
            RegisterCxItem(y);
        });

        grid.Add<CxButton>(y =>
        {
            y.Padding = new Padding(20, 10, 15, 10);
            y.Dock = DockStyle.Fill;
            y.DropDownMenu = CreateContextMenu(
                commandService.Commands.Where(i => i.CanExecuteWithHotKey),
                setValue);

            RegisterCxItem(y);
            RegisterCxItem(y.DropDownMenu);

            y.IconFont = IconFont;

            var binding = new Binding("Text", Config.UserConfig, propertyName, true, DataSourceUpdateMode.Never);
            binding.Format += (_, e) => e.Value = commandService.Commands
                .FirstOrDefault(x => x.Name == e.Value as string)?.Description ?? "[ BLANK ]";

            y.DataBindings.Add(binding);
        });
    }

    private CxContextMenu CreateContextMenu(IEnumerable<CommandBase> commands, Action<string?> setValue)
    {
        var menu = new CxContextMenu();
        var relayCommand = new RelayCommand(x => setValue((x as CommandBase)?.Name));

        menu.AddMenuItem("[ BLANK ]", relayCommand, null);
        menu.AddSeparator();

        foreach (var c in commands)
        {
            menu.AddMenuItem(c.Description, relayCommand, c);
        }

        return menu;
    }
}