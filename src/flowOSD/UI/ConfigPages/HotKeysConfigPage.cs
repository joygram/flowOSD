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
    private ICommandService commandService;

    public HotKeysConfigPage(IConfig config, CxTabListener tabListener, ICommandService commandService)
        : base(config, tabListener)
    {
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        Text = "HotKeys";

        this.Add<CxGrid>(0, 0, grid =>
        {
            grid.TabListener = TabListener;

            grid.MouseClick += OnMouseClick;
            grid.Padding = new Padding(10, 10, 10, 10);
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

            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Add<FlowLayoutPanel>(0, grid.RowStyles.Count - 1, 2, 1, panel =>
            {
                panel.MouseClick += OnMouseClick;
                panel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
                panel.AutoSize = true;
                panel.Margin = new Padding(0, 10, 5, 5);
                panel.Padding = new Padding(0);
                panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                panel.AutoSize = true;

                panel.Add<CxButton>(x =>
                {
                    x.TabListener = TabListener;
                    x.Padding = new Padding(20, 5, 20, 5);
                    x.Margin = new Padding(-2);

                    x.Text = "Disable all";
                    x.AutoSize = true;
                    x.Click += (sender, e) =>
                    {
                        config.UserConfig.AuraCommand = null;
                        config.UserConfig.FanCommand = null;
                        config.UserConfig.RogCommand = null;
                        config.UserConfig.CopyCommand = null;
                        config.UserConfig.PasteCommand = null;
                    };
                });

                panel.Add<CxButton>(x =>
                {
                    x.TabListener = TabListener;
                    x.Padding = new Padding(20, 5, 20, 5);
                    x.Margin = new Padding(-2);

                    x.Text = "Reset";
                    x.AutoSize = true;
                    x.Click += (sender, e) =>
                    {
                        config.UserConfig.AuraCommand = nameof(DisplayRefreshRateCommand);
                        config.UserConfig.FanCommand = nameof(ToggleBoostCommand);
                        config.UserConfig.RogCommand = nameof(PrintScreenCommand);
                        config.UserConfig.CopyCommand = null;
                        config.UserConfig.PasteCommand = null;
                    };
                });
            });
        });
    }

    private void Add(CxGrid grid, string text, string propertyName, Action<string?> setValue)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        grid.Add<CxLabel>(0, grid.RowCount - 1, x =>
        {
            x.MinimumSize = new Size(100, 30);
            x.TabListener = TabListener;
            x.AutoSize = true;
            x.Margin = new Padding(5, 20, 20, 20);
            x.Text = text;
            x.Anchor = AnchorStyles.Left;
            x.ForeColor = SystemColors.ControlText;
            x.UseClearType = true;
            x.TextAlign = ContentAlignment.MiddleCenter;
        });

        grid.Add<CxButton>(x =>
        {
            x.TabListener = TabListener;
            x.Padding = new Padding(10, 10, 15, 10);
            x.Dock = DockStyle.Fill;
            x.DropDownMenu = CreateContextMenu(
                commandService.Commands.Where(i => i.CanExecuteWithHotKey),
                setValue);

            x.BorderRadius = CornerRadius.Small;
            x.TextAlign = ContentAlignment.MiddleLeft;
            x.IconFont = IconFont;

            var binding = new Binding("Text", Config.UserConfig, propertyName, true, DataSourceUpdateMode.Never);
            binding.Format += (_, e) => e.Value = commandService.Commands
                .FirstOrDefault(x => x.Name == e.Value as string)?.Description ?? "[ BLANK ]";

            x.DataBindings.Add(binding);
        });
    }

    private CxContextMenu CreateContextMenu(IEnumerable<CommandBase> commands, Action<string?> setValue)
    {
        var menu = new CxContextMenu();
        menu.BorderRadius = CornerRadius.Small;

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