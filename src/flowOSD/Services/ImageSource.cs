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

using System.Reactive.Disposables;
using flowOSD.Api;

sealed class ImageSource : IImageSource, IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();

    private Dictionary<string, Image> images;
    private Dictionary<string, Icon> icons;

    public ImageSource()
    {
        images = new Dictionary<string, Image>();
        icons = new Dictionary<string, Icon>();
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
    }

    public Image GetImage(string name, int dpi, bool? isDarkTheme = null)
    {
        var image = default(Image);
        var scale = Math.Truncate(dpi / 96f * 100);

        do
        {
            image = GetImageByKey(GetImageKey(name, scale, isDarkTheme));

            if (image == null)
            {
                scale = Math.Round(Math.Floor(scale / 100) + 1) * 100;
            }
        }
        while (image == null && scale < 400);

        if (image == null)
        {
            throw new ApplicationException($"Image was not found: {name}{(isDarkTheme == null ? "" : " (dark: " + isDarkTheme + ") ")} @{dpi}.");
        }

        return image;
    }

    private Image GetImageByKey(string key)
    {
        if (!images.ContainsKey(key))
        {
            var assembly = typeof(Images).Assembly;

            var resourceName = $"flowOSD.Resources.{key}.png";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                images[key] = Image.FromStream(stream).DisposeWith(disposable);
            }
        }

        return images[key];
    }

    private static string GetImageKey(string name, double scale, bool? isDarkTheme)
    {
        return $"{GetThemeFolder(isDarkTheme)}_{(int)scale}.{name}";
    }

    private static string GetThemeFolder(bool? isDarkTheme)
    {
        if (isDarkTheme == null)
        {
            return string.Empty;
        }
        else
        {
            return isDarkTheme.Value ? "Dark." : "Light.";
        }
    }

    public Icon GetIcon(string name, int? dpi)
    {
        var key = dpi == null ? name : $"{name}-{dpi}";
        if (!icons.ContainsKey(key))
        {
            var assembly = typeof(Images).Assembly;

            var resourceName = $"flowOSD.Resources.{name}.ico";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new ApplicationException($"Icon was not found: {resourceName}.");
                }

                if (dpi == null)
                {
                    icons[key] = new Icon(stream).DisposeWith(disposable);
                }
                else
                {
                    var width = GetIconWidth(dpi.Value);
                    icons[key] = new Icon(stream, width, width).DisposeWith(disposable);
                }
            }
        }

        return icons[key];
    }

    private static int GetIconWidth(int dpi)
    {
        return dpi <= 192
            ? 16 * (dpi * 100 / 96) / 100
            : 64;
    }
}