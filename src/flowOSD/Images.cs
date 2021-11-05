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
namespace flowOSD
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Reactive.Disposables;
    using static Native;

    sealed class Images : IDisposable
    {
        public const string Tablet = "tablet";
        public const string TabletWhite = "tablet-white";
        public const string Notebook = "notebook";
        public const string NotebookWhite = "notebook-white";
        public const string Keyboard = "keyboard";

        private CompositeDisposable disposable = new CompositeDisposable();
        private Dictionary<string, Image> images;
        private Dictionary<string, Icon> icons;

        public Images()
        {
            images = new Dictionary<string, Image>();
            icons = new Dictionary<string, Icon>();
        }

        void IDisposable.Dispose()
        {
            disposable?.Dispose();
        }

        public Image GetImage(string name, int? dpi)
        {
            var key = dpi == null ? name : $"{name}-{dpi}";
            if (!images.ContainsKey(key))
            {
                var assembly = typeof(Images).Assembly;

                using (Stream stream = assembly.GetManifestResourceStream($"flowOSD.Resources.{key}.png"))
                {
                    images[key] = Image.FromStream(stream).DisposeWith(disposable);
                }
            }

            return images[key];
        }

        public Icon GetIcon(string name, int? dpi)
        {
            var key = dpi == null ? name : $"{name}-{dpi}";
            if (!icons.ContainsKey(key))
            {
                var assembly = typeof(Images).Assembly;

                using (Stream stream = assembly.GetManifestResourceStream($"flowOSD.Resources.{name}.ico"))
                {
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
            switch (dpi)
            {
                case 96:
                    return 16;
                case 120:
                    return 20;
                case 144:
                    return 24;
                case 168:
                    return 28;
                case 192:
                    return 32;
                default:
                    return 64;
            }
        }
    }
}