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
namespace flowOSD.Services;

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using flowOSD.Api;
using flowOSD.Api.Configs;
using flowOSD.Api.Hardware;
using flowOSD.Extensions;
using Microsoft.Win32;

sealed class ConfigService : IConfig, IDisposable
{
    private const string RUN_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private CompositeDisposable? disposable = new CompositeDisposable();
    private FileInfo configFile;

    public ConfigService()
    {
        AppFile = new FileInfo(typeof(ConfigService).Assembly.Location);
        AppFileInfo = FileVersionInfo.GetVersionInfo(AppFile.FullName);

        ProductName = AppFileInfo.ProductName ?? throw new ApplicationException("Product Name isn't set");
        ProductVersion = AppFileInfo.ProductVersion ?? throw new ApplicationException("Product Version isn't set");
        FileVersion = new Version(
            AppFileInfo.FileMajorPart,
            AppFileInfo.FileMinorPart,
            AppFileInfo.FileBuildPart,
            AppFileInfo.FilePrivatePart);

        IsPreRelease = Regex.IsMatch(ProductVersion, "[a-zA-Z]");

        DataDirectory = new DirectoryInfo(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFileInfo.ProductName!));

        if (!DataDirectory.Exists)
        {
            DataDirectory.Create();
        }

        configFile = new FileInfo(Path.Combine(DataDirectory.FullName, "config.json"));

        var poco = Load();

        Common = poco.Common ?? new CommonConfig();
        Common.RunAtStartup = GetStartupOption();
        Common.PropertyChanged
            .Where(x => x == nameof(Common.RunAtStartup))
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(_ => UpdateStartupOption(Common.RunAtStartup))
            .DisposeWith(disposable);
        Common.PropertyChanged
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(x => Save())
            .DisposeWith(disposable);

        Notifications = poco.Notifications ?? new NotificationsConfig();
        Notifications.PropertyChanged
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(x => Save())
            .DisposeWith(disposable);

        HotKeys = poco.HotKeys ?? new HotKeysConfig();
        HotKeys.PropertyChanged
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(x => Save(x))
            .DisposeWith(disposable);

        var service = new System.ServiceProcess.ServiceController("ASUSOptimization");
        UseOptimizationMode = service.Status != System.ServiceProcess.ServiceControllerStatus.Stopped;
    }

    public CommonConfig Common { get; }

    public NotificationsConfig Notifications { get; }

    public HotKeysConfig HotKeys { get; }

    public FileInfo AppFile { get; }

    public FileVersionInfo AppFileInfo { get; }

    public DirectoryInfo DataDirectory { get; }

    public bool UseOptimizationMode { get; }

    public bool IsPreRelease { get; }

    public string ProductName { get; }

    public string ProductVersion { get; }

    public Version FileVersion { get; }

    public void Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private POCO Load()
    {
        try
        {
            using var stream = configFile.OpenRead();

            var options = new JsonSerializerOptions { WriteIndented = true };
            options.Converters.Add(new NotificationConfigConverter());
            options.Converters.Add(new HotKeysConfigConverter());

            return JsonSerializer.Deserialize<POCO>(stream, options) ?? new POCO();
        }
        catch (Exception)
        {
            return new POCO();
        }
    }

    private void Save(string propertyName = null)
    {
        using var stream = configFile.Create();

        var options = new JsonSerializerOptions { WriteIndented = true };
        options.Converters.Add(new NotificationConfigConverter());
        options.Converters.Add(new HotKeysConfigConverter());

        var poco = new POCO
        {
            Common = this.Common,
            Notifications = this.Notifications,
            HotKeys = this.HotKeys
        };

        JsonSerializer.Serialize<POCO>(stream, poco, options);
    }

    private bool GetStartupOption()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);

        return key?.GetValue(AppFileInfo.ProductName) != null;
    }

    private void UpdateStartupOption(bool runAtStartup)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);

        if (key == null)
        {
            throw new ApplicationException("Can't write to Windows registry");
        }

        if (runAtStartup)
        {
            key.SetValue(AppFileInfo.ProductName!, Application.ExecutablePath);
        }
        else
        {
            key.DeleteValue(AppFileInfo.ProductName!, false);
        }
    }

    private class POCO
    {
        public CommonConfig? Common { get; set; }

        public NotificationsConfig? Notifications { get; set; }

        public HotKeysConfig? HotKeys { get; set; }
    }

    private class NotificationConfigConverter : JsonConverter<NotificationsConfig>
    {
        public override NotificationsConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var config = new NotificationsConfig();

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new ApplicationException("Config file is corrupted");
            }

            while (reader.Read() && reader.TokenType == JsonTokenType.String)
            {
                var v = reader.GetString();
                if (Enum.TryParse<NotificationType>(v, out var type))
                {
                    config[type] = true;
                }
            }

            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new ApplicationException("Config file is corrupted");
            }

            return config;
        }

        public override void Write(Utf8JsonWriter writer, NotificationsConfig value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var i in Enum.GetValues<NotificationType>())
            {
                if (value[i])
                {
                    writer.WriteStringValue(Enum.GetName(i));
                }
            }

            writer.WriteEndArray();
        }
    }

    private class HotKeysConfigConverter : JsonConverter<HotKeysConfig>
    {
        private const string KEY = "Key";

        public override HotKeysConfig? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var config = new HotKeysConfig();

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new ApplicationException("Config file is corrupted");
            }

            var item = new Dictionary<string, string>();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();

                    if (propertyName != null && reader.Read()
                        && (reader.TokenType == JsonTokenType.String || reader.TokenType == JsonTokenType.Null))
                    {
                        if (reader.TokenType != JsonTokenType.Null)
                        {
                            item[propertyName] = reader.GetString()!;
                        }
                    }
                    else
                    {
                        throw new ApplicationException("Config file is corrupted");
                    }
                }

                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    if (item.TryGetValue(KEY, out var keyRaw) && Enum.TryParse<AtkKey>(keyRaw, out var key)
                        && item.TryGetValue(nameof(HotKeysConfig.Command.Name), out var commandName))
                    {
                        config[key] = new HotKeysConfig.Command(
                            commandName,
                            item.ContainsKey(nameof(HotKeysConfig.Command.Parameter)) ? item[nameof(HotKeysConfig.Command.Parameter)] : null);
                    }
                    else
                    {
                        throw new ApplicationException("Config file is corrupted");
                    }

                    item = new Dictionary<string, string>();
                }
            }

            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new ApplicationException("Config file is corrupted");
            }

            return config;
        }

        public override void Write(Utf8JsonWriter writer, HotKeysConfig value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();

            foreach (var i in Enum.GetValues<AtkKey>())
            {
                var item = value[i];
                if (item != null)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName(KEY);
                    writer.WriteStringValue(Enum.GetName(i));

                    writer.WritePropertyName(nameof(HotKeysConfig.Command.Name));
                    writer.WriteStringValue(item.Name);

                    writer.WritePropertyName(nameof(HotKeysConfig.Command.Parameter));
                    writer.WriteStringValue(item.Parameter?.ToString());

                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
        }
    }
}