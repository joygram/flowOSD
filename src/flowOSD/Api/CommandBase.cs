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

namespace flowOSD.Api;

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using System.Windows.Input;

public abstract class CommandBase : ICommand, IDisposable, INotifyPropertyChanged
{
    private string text, description;
    private bool enabled, isChecked;

    protected CommandBase()
    {
        text = Name;
        description = string.Empty;
    }

    public string Text
    {
        get => text;
        protected set => SetProperty(ref text, value);
    }

    public string Description
    {
        get => description;
        protected set => SetProperty(ref description, value);
    }

    public bool Enabled
    {
        get => enabled;
        protected set
        {
            if (value == enabled)
            {
                return;
            }

            SetProperty(ref enabled, value);
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public virtual IList<ParameterInfo> Parameters => new ParameterInfo[0];

    public bool IsChecked
    {
        get => isChecked;
        protected set
        {
            if (value == isChecked)
            {
                return;
            }

            SetProperty(ref isChecked, value);
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public abstract string Name { get; }

    public virtual bool CanExecuteWithHotKey => true;

    protected CompositeDisposable? Disposable { get; private set; } = new CompositeDisposable();

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? CanExecuteChanged;

    public abstract void Execute(object? parameter = null);

    public virtual bool CanExecute(object? parameter)
    {
        return Enabled;
    }

    public void Dispose()
    {
        if (Disposable != null)
        {
            Disposable.Dispose();
            Disposable = null;
        }
    }

    protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(property, value))
        {
            property = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public readonly record struct ParameterInfo(string Value, string Description);
}