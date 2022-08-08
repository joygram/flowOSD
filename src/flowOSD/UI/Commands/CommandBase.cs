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
namespace flowOSD.UI.Commands;

using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using flowOSD.Api;

abstract class CommandBase : ICommand, IDisposable
{
    private string text, description;
    private bool enabled;

    public abstract string Name { get; }

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
        protected set => SetProperty(ref enabled, value);
    }

    protected CompositeDisposable Disposable { get; private set; } = new CompositeDisposable();

    private void SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
    {
        if (!Equals(property, value))
        {
            property = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public abstract void Execute(object parameter = null);

    public void Dispose()
    {
        if (Disposable != null)
        {
            Disposable.Dispose();
            Disposable = null;
        }
    }
}