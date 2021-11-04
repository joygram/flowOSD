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
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Reactive.Disposables;
    using System.Reflection;
    using System.Windows.Forms;
    using static Extensions;

    sealed class AppAbout : Form
    {
        public static void ShowForm()
        {
            if (instance != null)
            {
                instance.Activate();
            }
            else
            {
                instance = new AppAbout();
                instance.Show();
            }
        }

        public static string Title => $"{fileVersionInfo.ProductName} ({fileVersionInfo.ProductVersion})";

        private static AppAbout instance;
        private static FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetCallingAssembly().Location);

        private CompositeDisposable disposable = new CompositeDisposable();

        private AppAbout()
        {
            Init();
        }

        protected override void OnClosed(EventArgs e)
        {
            disposable?.Dispose();
            disposable = null;

            instance = null;

            base.OnClosed(e);
        }

        private void Init()
        {
            this.Text = "About";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(690, 620);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;

            this.Add(Create<TableLayoutPanel>(x =>
            {
                x.Dock = DockStyle.Fill;
                x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                x.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }).DisposeWith(disposable)
            .Add(0, 0, Create<PictureBox>(x =>
                {
                    x.Image = Icon
                        .ExtractAssociatedIcon(Assembly.GetCallingAssembly().Location)
                        .ToBitmap().DisposeWith(disposable);
                    x.Size = new Size(64, 64);
                    x.Margin = new Padding(10, 20, 10, 0);
                    x.SizeMode = PictureBoxSizeMode.Zoom;
                }))
            .Add(1, 0, Create<FlowLayoutPanel>(x =>
                 {
                     x.Dock = DockStyle.Fill;
                     x.FlowDirection = FlowDirection.TopDown;
                     x.Padding = new Padding(10);
                 }).DisposeWith(disposable).Add(
                    Create<Label>(x =>
                    {
                        x.Text = fileVersionInfo.ProductName;
                        x.Margin = new Padding(0, 0, 0, 10);
                        x.Font = new Font("Segoe UI Light", 18, FontStyle.Bold)
                            .DisposeWith(disposable);
                        x.AutoSize = true;
                    }).DisposeWith(disposable),
                    Create<Label>(x =>
                    {
                        x.Text = $"Version: {fileVersionInfo.ProductVersion}";
                        x.AutoSize = true;
                        x.Margin = new Padding(0, 3, 0, 3);
                    }).DisposeWith(disposable),
                    Create<Label>(x =>
                    {
                        x.Text = $"{fileVersionInfo.LegalCopyright}";
                        x.AutoSize = true;
                        x.Margin = new Padding(0, 3, 0, 3);
                    }).DisposeWith(disposable),
                    Create<Label>(x =>
                    {
                        x.Text = $"{fileVersionInfo.Comments}";
                        x.AutoSize = true;
                        x.Margin = new Padding(0, 15, 0, 3);
                    }).DisposeWith(disposable)
                ))
            .Add(1, 1, Create<Button>(x =>
                {
                    x.Text = "OK";
                    x.AutoSize = true;
                    x.Padding = new Padding(15, 3, 15, 3);
                    x.Margin = new Padding(0, 0, 15, 15);
                    x.Anchor = AnchorStyles.Right;
                    x.Click += (sender, e) => Close();
                }).DisposeWith(disposable)
            ));
        }
    }
}