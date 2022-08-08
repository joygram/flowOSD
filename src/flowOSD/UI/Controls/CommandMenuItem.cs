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
namespace flowOSD.UI.Commands;

using System.Reactive.Linq;
using flowOSD.Api;

sealed class CommandMenuItem : ToolStripMenuItem
{
    private ICommandManager commandManager;
    private string commandName;

    private CommandBinding commandBinding;

    public bool BindProperties { get; set; } = true;

    public string CommandName
    {
        get => commandName;
        set
        {
            commandName = value;
            BindCommand();
        }
    }

    public object CommandParameter
    {
        get;
        set;
    }

    public ICommandManager CommandManager
    {
        get => commandManager;
        set
        {
            commandManager = value;

            BindCommand();
        }
    }

    protected override void OnClick(EventArgs e)
    {
        if (commandBinding != null)
        {
            commandBinding.Command.Execute(CommandParameter);
        }
    }

    private void BindCommand()
    {
        if (commandBinding != null)
        {
            commandBinding.Dispose();
            commandBinding = null;
        }

        var command = CommandManager?.Resolve(CommandName);
        if (command != null)
        {
            commandBinding = new CommandBinding(this, command);
        }
    }

    sealed class CommandBinding : IDisposable
    {
        private CommandMenuItem menuItem;

        public CommandBinding(CommandMenuItem menuItem, ICommand command)
        {
            this.menuItem = menuItem;

            Command = command;
            Command.PropertyChanged += OnPropertyChanged;

            InitMenuItem();
        }

        public ICommand Command { get; private set; }

        public void Dispose()
        {
            if (Command != null)
            {
                Command.PropertyChanged -= OnPropertyChanged;
                Command = null;
            }
        }

        private void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!menuItem.BindProperties)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(ICommand.Text):
                    {
                        menuItem.Text = Command.Text;
                        break;
                    }

                case nameof(ICommand.Enabled):
                    {
                        menuItem.Enabled = Command.Enabled;
                        break;
                    }

                case "":
                case null:
                    {
                        InitMenuItem();

                        break;
                    }

            }
        }

        private void InitMenuItem()
        {
            if (!menuItem.BindProperties)
            {
                return;
            }

            menuItem.Text = Command.Text;
            menuItem.Enabled = Command.Enabled;
        }
    }
}