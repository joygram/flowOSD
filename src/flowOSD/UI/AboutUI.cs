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
namespace flowOSD.UI
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reflection;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using flowOSD.Api;
    using static Extensions;
    using static Native;

    public class AboutUI : IDisposable
    {
        private CompositeDisposable disposable = new CompositeDisposable();

        private FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(typeof(AboutUI).Assembly.Location);
        private Window instance;

        void IDisposable.Dispose()
        {
            disposable?.Dispose();
            disposable = null;
        }

        public void Show()
        {
            if (instance != null)
            {
                instance.Activate();
            }
            else
            {
                instance = new Window(this).DisposeWith(disposable);
                instance.Show();
            }
        }

        public string AppTitle => $"{fileVersionInfo.ProductName} ({fileVersionInfo.ProductVersion})";

        private sealed class Window : Form
        {
            private CompositeDisposable disposable = new CompositeDisposable();
            private AboutUI owner;

            public Window(AboutUI owner)
            {
                this.owner = owner;

                this.Text = "About";
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.ShowIcon = false;
                this.ShowInTaskbar = false;
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                // this.AutoScaleMode = AutoScaleMode.Dpi;

                var scale = GetDpiForWindow(Handle) / 96f;
                this.Font = new Font("Segoe UI", 12 * scale, GraphicsUnit.Pixel);

                this.Add(Create<TableLayoutPanel>(x =>
                {
                    x.Dock = DockStyle.Fill;
                    x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                    x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    x.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                }).DisposeWith(disposable)
                    .Add<TableLayoutPanel, PictureBox>(0, 0, 1, 3, x =>
                    {
                        x.Image = Icon
                            .ExtractAssociatedIcon(Assembly.GetCallingAssembly().Location)
                            .ToBitmap().DisposeWith(disposable);
                        x.Size = new Size(64, 64);
                        x.Margin = new Padding(20);
                        x.SizeMode = PictureBoxSizeMode.Zoom;
                    }).DisposeWith(disposable)
                    .Add<TableLayoutPanel, Label>(1, 0, x =>
                    {
                        x.Text = $"{owner.fileVersionInfo.ProductName}";
                        x.Font = new Font(Font.FontFamily, 20);
                        x.AutoSize = true;
                        x.Margin = new Padding(0, 3, 0, 3);
                    }).DisposeWith(disposable)
                    .Add<TableLayoutPanel, Label>(1, 1, x =>
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine($"Version: {owner.fileVersionInfo.ProductVersion}");
                        sb.AppendLine($"{owner.fileVersionInfo.LegalCopyright}");
                        sb.AppendLine();
                        sb.AppendLine($"{owner.fileVersionInfo.Comments}");
                        sb.AppendLine();
                        sb.AppendLine($"Runtime: {Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName}");

                        x.Text = sb.ToString();
                        x.AutoSize = true;
                        x.Margin = new Padding(10, 15, 20, 3);
                    }).DisposeWith(disposable)
                    .Add<TableLayoutPanel, Button>(1, 2, x =>
                    {
                        x.Text = "OK";
                        x.AutoSize = true;
                        x.Padding = new Padding(15, 3, 15, 3);
                        x.Margin = new Padding(0, 0, 20, 20);
                        x.Anchor = AnchorStyles.Right;
                        x.Click += (sender, e) => Close();
                    }).DisposeWith(disposable)
                );
            }

            private void UpdateSize(int dpi)
            {
                var scale = dpi / 96f;
                this.Size = new Size((int)(400 * scale), (int)(300 * scale));
            }

            protected override void OnShown(EventArgs e)
            {
                UpdateSize(GetDpiForWindow(Handle));

                base.OnShown(e);
            }

            protected override void OnDpiChanged(DpiChangedEventArgs e)
            {
                UpdateSize(e.DeviceDpiNew);

                base.OnDpiChanged(e);
            }

            protected override void OnClosing(CancelEventArgs e)
            {
                owner.disposable.Remove(owner.instance);
                owner.instance = null;

                base.OnClosing(e);
            }

        }
    }
}