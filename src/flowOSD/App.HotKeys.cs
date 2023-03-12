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
namespace flowOSD;

using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using flowOSD.Api;
using flowOSD.Services;
using flowOSD.UI;
using flowOSD.UI.Commands;
using flowOSD.UI.Components;
using static Extensions;

partial class App
{
    private void InitHotKeys()
    {
        hotKeyManager = new HotKeyManager(commandManager);
        config.UserConfig.PropertyChanged.Subscribe(propertyName =>
        {
            switch (propertyName)
            {
                case nameof(UserConfig.AuraCommand):
                    hotKeyManager.Register(AtkKey.Aura, config.UserConfig.AuraCommand);
                    break;

                case nameof(UserConfig.FanCommand):
                    hotKeyManager.Register(AtkKey.Fan, config.UserConfig.FanCommand);
                    break;

                case nameof(UserConfig.RogCommand):
                    hotKeyManager.Register(AtkKey.Rog, config.UserConfig.RogCommand);
                    break;

                case nameof(UserConfig.CopyCommand):
                    hotKeyManager.Register(AtkKey.Copy, config.UserConfig.CopyCommand);
                    break;

                case nameof(UserConfig.PasteCommand):
                    hotKeyManager.Register(AtkKey.Paste, config.UserConfig.PasteCommand);
                    break;

                case "":
                case null:
                    RegisterHotKeys();
                    break;
            }
        }).DisposeWith(disposable);

        RegisterHotKeys();

        keyboard.KeyPressed
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(hotKeyManager.ExecuteCommand)
            .DisposeWith(disposable);
    }

    private void RegisterHotKeys()
    {
        hotKeyManager.Register(AtkKey.Aura, config.UserConfig.AuraCommand);
        hotKeyManager.Register(AtkKey.Fan, config.UserConfig.FanCommand);
        hotKeyManager.Register(AtkKey.Rog, config.UserConfig.RogCommand);
        hotKeyManager.Register(AtkKey.Copy, config.UserConfig.CopyCommand);
        hotKeyManager.Register(AtkKey.Paste, config.UserConfig.PasteCommand);
    }
}