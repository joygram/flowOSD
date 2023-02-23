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
namespace flowOSD.Services;

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using flowOSD.Api;
using Microsoft.Win32;

sealed class Config : IConfig, IDisposable
{
    private const string RUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private CompositeDisposable disposable = new CompositeDisposable();
    private FileInfo configFile;
    private Lazy<UserConfig> userConfig;

    public Config()
    {
        AppFile = new FileInfo(typeof(Config).Assembly.Location);
        AppFileInfo = FileVersionInfo.GetVersionInfo(AppFile.FullName);
        DataDirectory = new DirectoryInfo(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFileInfo.ProductName));

        if (!DataDirectory.Exists)
        {
            DataDirectory.Create();
        }

        configFile = new FileInfo(Path.Combine(DataDirectory.FullName, "config.json"));

        userConfig = new Lazy<UserConfig>(() =>
        {
            var config = configFile.Exists
                ? Load()
                : new UserConfig();
            config.RunAtStartup = GetStartupOption();

            config.PropertyChanged
                .Where(x => x == nameof(UserConfig.RunAtStartup))
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(_ => UpdateStartupOption(UserConfig.RunAtStartup))
                .DisposeWith(disposable);

            config.PropertyChanged
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(x => Save())
                .DisposeWith(disposable);

            return config;
        }, true);
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    public UserConfig UserConfig { get => userConfig.Value; }

    public FileInfo AppFile { get; }

    public FileVersionInfo AppFileInfo { get; }

    public DirectoryInfo DataDirectory { get; }

    private UserConfig Load()
    {
        try
        {
            using (var stream = configFile.OpenRead())
            {
                return JsonSerializer.Deserialize<UserConfig>(stream);
            }
        }
        catch (Exception)
        {
            return new UserConfig();
        }
    }

    private void Save()
    {
        using (var stream = configFile.Create())
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            JsonSerializer.Serialize<UserConfig>(stream, UserConfig, options);
        }
    }

    private bool GetStartupOption()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
        {
            return key.GetValue(AppFileInfo.ProductName) != null;
        }
    }

    private void UpdateStartupOption(bool runAtStartup)
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true))
        {
            if (runAtStartup)
            {
                key.SetValue(AppFileInfo.ProductName, Application.ExecutablePath);
            }
            else
            {
                key.DeleteValue(AppFileInfo.ProductName, false);
            }
        }
    }
}