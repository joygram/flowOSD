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
using static flowOSD.Extensions.Common;

internal class HotKeysConfigPage : ConfigPageBase
{
    private ICommandService commandService;

    public HotKeysConfigPage(IConfig config, CxTabListener tabListener, ICommandService commandService)
        : base(config, tabListener)
    {
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        Text = "HotKeys";

        var commands = commandService.Commands.Where(i => i.CanExecuteWithHotKey);

        AddConfig<string>(
            "`AURA`",
            nameof(config.UserConfig.AuraCommand),
            value => commandService.Commands.FirstOrDefault(x => x.Name == value)?.Description ?? "[ BLANK ]",
            () => CreateContextMenu(commands, value => config.UserConfig.AuraCommand = value));
        AddConfig<string>(
            "`FAN`", 
            nameof(config.UserConfig.FanCommand),
            value => commandService.Commands.FirstOrDefault(x => x.Name == value)?.Description ?? "[ BLANK ]",
            () => CreateContextMenu(commands, value => config.UserConfig.FanCommand = value));
        AddConfig<string>(
            "`ROG`", 
            nameof(config.UserConfig.RogCommand),
            value => commandService.Commands.FirstOrDefault(x => x.Name == value)?.Description ?? "[ BLANK ]",
            () => CreateContextMenu(commands, value => config.UserConfig.RogCommand = value));
        AddConfig<string>(
            "`Fn` + `C`", 
            nameof(config.UserConfig.CopyCommand),
            value => commandService.Commands.FirstOrDefault(x => x.Name == value)?.Description ?? "[ BLANK ]",
            () => CreateContextMenu(commands, value => config.UserConfig.CopyCommand = value));
        AddConfig<string>(
            "`Fn` + `V`", 
            nameof(config.UserConfig.PasteCommand),
            value => commandService.Commands.FirstOrDefault(x => x.Name == value)?.Description ?? "[ BLANK ]",
            () => CreateContextMenu(commands, value => config.UserConfig.PasteCommand = value));

        this.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        this.Add<FlowLayoutPanel>(0, RowStyles.Count - 1, 2, 1, panel =>
        {
            panel.MouseClick += OnMouseClick;
            panel.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            panel.AutoSize = true;
            panel.Margin = new Padding(0, 10, 0, 5);
            panel.Padding = new Padding(0);
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.AutoSize = true;

            panel.Add<CxButton>(x =>
            {
                x.BorderRadius = IsWindows11 ? CornerRadius.Small : CornerRadius.Off;
                x.TabListener = TabListener;
                x.Padding = new Padding(20, 5, 5, 5);
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
                x.BorderRadius = IsWindows11 ? CornerRadius.Small : CornerRadius.Off;
                x.TabListener = TabListener;
                x.Padding = new Padding(20, 5, 5, 5);
                x.Margin = new Padding(-2);

                x.Text = "Reset";
                x.AutoSize = true;
                x.Click += (sender, e) =>
                {
                    config.UserConfig.AuraCommand = nameof(DisplayRefreshRateCommand);
                    config.UserConfig.FanCommand = nameof(ToggleBoostCommand);
                    config.UserConfig.RogCommand = nameof(MainUICommand);
                    config.UserConfig.CopyCommand = null;
                    config.UserConfig.PasteCommand = null;
                };
            });
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