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
namespace flowOSD.UI.ConfigPages;

using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;
using flowOSD.UI.Commands;
using flowOSD.UI.Components;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using static flowOSD.Extensions.Common;

internal class HotKeysConfigPage : ConfigPageBase
{
    private const string NO_COMMAND_DESCIPTION = "[ BLANK ]";

    private CompositeDisposable? disposable = new CompositeDisposable();

    private ICommandService commandService;
    private Dictionary<AtkKey, CxButton> buttons;

    public HotKeysConfigPage(IConfig config, CxTabListener tabListener, ICommandService commandService)
        : base(config, tabListener)
    {
        this.commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

        buttons = new Dictionary<AtkKey, CxButton>();

        Text = "HotKeys";

        CxButton? button;

        if (AddConfig("`Fn` + `F4`  ( `AURA` )", () => CreateContextMenu(AtkKey.Aura)).FindChild(out button) && button != null)
        {
            buttons[AtkKey.Aura] = button;
        }

        if (AddConfig("`Fn` + `F5`  ( `FAN` )", () => CreateContextMenu(AtkKey.Fan)).FindChild(out button) && button != null)
        {
            buttons[AtkKey.Fan] = button;
        }

        if (AddConfig("`ROG`", () => CreateContextMenu(AtkKey.Rog)).FindChild(out button) && button != null)
        {
            buttons[AtkKey.Rog] = button;
        }

        if (AddConfig("`Fn` + `C`", () => CreateContextMenu(AtkKey.Copy)).FindChild(out button) && button != null)
        {
            buttons[AtkKey.Copy] = button;
        }

        if (AddConfig("`Fn` + `V`", () => CreateContextMenu(AtkKey.Paste)).FindChild(out button) && button != null)
        {
            buttons[AtkKey.Paste] = button;
        }

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
                    config.HotKeys.Clear();
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
                    config.HotKeys[AtkKey.Aura] = new HotKeysConfig.Command(nameof(DisplayRefreshRateCommand));
                    config.HotKeys[AtkKey.Fan] = new HotKeysConfig.Command(nameof(ToggleBoostCommand));
                    config.HotKeys[AtkKey.Rog] = new HotKeysConfig.Command(nameof(MainUICommand));
                    config.HotKeys[AtkKey.Copy] = null;
                    config.HotKeys[AtkKey.Paste] = null;
                };
            });
        });

        config.HotKeys.KeyChanged
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(key => Update(key))
            .DisposeWith(disposable);

        Update(null);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            disposable?.Dispose();
            disposable = null;
        }

        base.Dispose(disposing);
    }

    private void Update(AtkKey? key)
    {
        if (key != null && buttons.TryGetValue(key.Value, out var button))
        {
            button.Text = GetDescription(Config.HotKeys[key.Value]);
        }

        if (key == null)
        {
            foreach (var k in buttons.Keys)
            {
                buttons[k].Text = GetDescription(Config.HotKeys[k]);
            }
        }
    }

    private string? GetDescription(HotKeysConfig.Command? commandInfo)
    {
        if (commandInfo == null)
        {
            return NO_COMMAND_DESCIPTION;
        }

        var command = commandService.Commands.FirstOrDefault(i => i.Name == commandInfo.Name);

        if (command?.Parameters.Count > 0)
        {
            return $"{command.Description} - {command.Parameters.FirstOrDefault(i => i.Value == commandInfo.Parameter).Description}";
        }
        else
        {
            return command?.Description ?? NO_COMMAND_DESCIPTION;
        }
    }

    private CxContextMenu CreateContextMenu(AtkKey key)
    {
        var menu = new CxContextMenu();
        menu.BorderRadius = CornerRadius.Small;

        var relayCommand = new RelayCommand(x =>
        {
            if (x is CommandInfo info)
            {
                Config.HotKeys[key] = new HotKeysConfig.Command(info.Name, info.Parameter);
            }
        });

        menu.AddMenuItem(NO_COMMAND_DESCIPTION, relayCommand, null);
        menu.AddSeparator();

        foreach (var c in commandService.Commands)
        {
            if (c.Parameters.Count > 0)
            {
                foreach (var p in c.Parameters)
                {
                    menu.AddMenuItem(
                        $"{c.Description} - {p.Description}",
                        relayCommand,
                        new CommandInfo(c.Description, c.Name, p.Value));
                }
            }
            else
            {
                menu.AddMenuItem(
                    c.Description,
                    relayCommand,
                    new CommandInfo(c.Description, c.Name, null));
            }
        }

        return menu;
    }

    private readonly record struct CommandInfo(string Description, string Name, string? Parameter);
}