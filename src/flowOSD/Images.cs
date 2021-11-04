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
    using System.Drawing;
    using System.IO;
    using System.Reactive.Disposables;

    sealed class Images : IDisposable
    {
        private CompositeDisposable disposable;

        void IDisposable.Dispose()
        {
            disposable?.Dispose();
        }

        public Image Keyboard { get; private set; }

        public Icon Notebook { get; private set; }

        public Icon NotebookWhite { get; private set; }

        public Icon Tablet { get; private set; }

        public Icon TabletWhite { get; private set; }


        public void Load()
        {
            disposable?.Dispose();
            disposable = new CompositeDisposable();

            var assembly = typeof(Images).Assembly;

            using (Stream stream = assembly.GetManifestResourceStream("flowOSD.Resources.Keyboard24.png"))
            {
                Keyboard = Image.FromStream(stream).DisposeWith(disposable);
            }

            using (Stream stream = assembly.GetManifestResourceStream("flowOSD.Resources.notebook.ico"))
            {
                Notebook = new Icon(stream).DisposeWith(disposable);
            }

            using (Stream stream = assembly.GetManifestResourceStream("flowOSD.Resources.notebook-white.ico"))
            {
                NotebookWhite = new Icon(stream, 24, 24).DisposeWith(disposable);
            }

            using (Stream stream = assembly.GetManifestResourceStream("flowOSD.Resources.tablet.ico"))
            {
                Tablet = new Icon(stream).DisposeWith(disposable);
            }

            using (Stream stream = assembly.GetManifestResourceStream("flowOSD.Resources.tablet-white.ico"))
            {
                TabletWhite = new Icon(stream, 24, 24).DisposeWith(disposable);
            }
        }
    }
}