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
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Reactive.Disposables;
    using System.Reflection;
    using System.Text;
    using System.Windows.Forms;
    using static Extensions;
    using static Native;

    static class AppAbout
    {
        public static void Show()
        {
            if (instance.Visible)
            {
                instance.Activate();
            }
            else
            {
                instance.Visible = true;
            }
        }

        public static string Title => $"{fileVersionInfo.ProductName} ({fileVersionInfo.ProductVersion})";

        private readonly static FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetCallingAssembly().Location);
        private readonly static Window instance = new Window();

        private sealed class Window : Form
        {
            private CompositeDisposable disposable = new CompositeDisposable();

            public Window()
            {
                this.Text = "About";
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.ShowIcon = false;
                this.ShowInTaskbar = false;
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.AutoScaleMode = AutoScaleMode.Dpi;

                UpdateMinSize(GetDpiForWindow(Handle));

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
                        x.Text = $"{fileVersionInfo.ProductName}";
                        x.Font = new Font(Font.FontFamily, 20);
                        x.AutoSize = true;
                        x.Margin = new Padding(0, 3, 0, 3);
                    }).DisposeWith(disposable)
                    .Add<TableLayoutPanel, Label>(1, 1, x =>
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine($"Version: {fileVersionInfo.ProductVersion}");
                        sb.AppendLine($"{fileVersionInfo.LegalCopyright}");
                        sb.AppendLine();
                        sb.Append($"{fileVersionInfo.Comments}");

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

            private void UpdateMinSize(int dpi)
            {
                var x = dpi / 96f;
                this.MinimumSize = new Size((int)(450 * x), (int)(400 * x));
            }

            protected override void OnDpiChanged(DpiChangedEventArgs e)
            {
                UpdateMinSize(e.DeviceDpiNew);

                base.OnDpiChanged(e);
            }

            protected override void OnClosing(CancelEventArgs e)
            {
                e.Cancel = true;
                Hide();

                base.OnClosing(e);
            }

        }
    }
}