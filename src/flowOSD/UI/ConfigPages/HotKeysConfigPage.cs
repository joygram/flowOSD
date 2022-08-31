/*  Copyright © 2021-2022, Albert Akhmetov <akhmetov@live.com>   
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
using flowOSD.UI.Commands;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;

internal class HotKeysConfigPage : TableLayoutPanel
{
    private CompositeDisposable disposable = new CompositeDisposable();
    private IConfig config;
    private ICommandManager commandManager;
    private HotKeyCommand[] hotKeyCommands;

    public HotKeysConfigPage(IConfig config, ICommandManager commandManager)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));

        var c = new List<HotKeyCommand>();
        c.Add(new HotKeyCommand("", null));
        c.AddRange(commandManager.Commands.Select(i => new HotKeyCommand(i.Description, i.Name)));
        hotKeyCommands = c.ToArray();

        Text = "HotKeys";

        ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        RowStyles.Add(new RowStyle(SizeType.AutoSize));
        RowStyles.Add(new RowStyle(SizeType.AutoSize));
        RowStyles.Add(new RowStyle(SizeType.AutoSize));
        RowStyles.Add(new RowStyle(SizeType.AutoSize));
        RowStyles.Add(new RowStyle(SizeType.AutoSize));
        RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Add(0, "AURA", () => config.UserConfig.AuraCommand, value => config.UserConfig.AuraCommand = value);
        Add(1, "FAN", () => config.UserConfig.FanCommand, value => config.UserConfig.FanCommand = value);
        Add(2, "ROG", () => config.UserConfig.RogCommand, value => config.UserConfig.RogCommand = value);
        Add(3, "Fn + C", () => config.UserConfig.CopyCommand, value => config.UserConfig.CopyCommand = value);
        Add(4, "Fn + V", () => config.UserConfig.PasteCommand, value => config.UserConfig.PasteCommand = value);

        this.Add<FlowLayoutPanel>(0, 5, 2, 1, x =>
        {
            x.Dock = DockStyle.Right;
            x.AutoSize = true;
            x.Margin = new Padding(0);

            x.Add<Button>(y =>
            {
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
            });

            x.Add<Button>(y =>
            {
                y.Text = "Reset";
                y.AutoSize = true;
                y.Margin = new Padding(5, 0, 0, 0);
                y.Click += (sender, e) =>
                {
                    config.UserConfig.AuraCommand = nameof(ToggleRefreshRateCommand);
                    config.UserConfig.FanCommand = nameof(ToggleBoostCommand);
                    config.UserConfig.RogCommand = nameof(PrintScreenCommand);
                    config.UserConfig.CopyCommand = null;
                    config.UserConfig.PasteCommand = null;
                };
            });
        });
    }

    private void Add(int row, string text, Func<string> getValue, Action<string> setValue)
    {
        this.Add<Label>(0, row, y =>
        {
            y.AutoSize = true;
            y.Margin = new Padding(15, 5, 0, 15);
            y.Text = text;
            y.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            y.ForeColor = SystemColors.ControlText;

            y.DisposeWith(disposable);
        });

        this.Add<ComboBox>(1, row, y =>
        {
            y.AutoSize = true;
            y.Margin = new Padding(15, 5, 0, 15);
            y.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            y.ForeColor = SystemColors.ControlText;

            y.Items.AddRange(hotKeyCommands);
            y.DisplayMember = nameof(HotKeyCommand.Text);
            y.ValueMember = nameof(HotKeyCommand.CommandName);

            y.DropDownStyle = ComboBoxStyle.DropDownList;

            y.SelectedItem = hotKeyCommands.FirstOrDefault(x => x.CommandName == getValue());
            y.SelectedIndexChanged += (sender, e) =>
            {
                setValue((y.SelectedItem as HotKeyCommand)?.CommandName);
            };

            config.UserConfig.PropertyChanged
                .Subscribe(_ => y.SelectedItem = hotKeyCommands.FirstOrDefault(x => x.CommandName == getValue()))
                .DisposeWith(disposable);

            y.DisposeWith(disposable);
        });
    }

    private class HotKeyCommand
    {
        public HotKeyCommand(string text, string commandName)
        {
            Text = text;
            CommandName = commandName;
        }

        public string Text { get; }

        public string CommandName { get; }
    }
}