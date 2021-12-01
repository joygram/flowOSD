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
namespace flowOSD.UI;

using System.Reactive.Disposables;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using flowOSD.Api;
using static Extensions;
using static Native;

public class AboutUI : IDisposable
{
    private Window instance;
    private IConfig config;

    public AboutUI(IConfig config)
    {
        this.config = config;
    }

    void IDisposable.Dispose()
    {
        if (instance != null && !instance.IsDisposed)
        {
            instance.Dispose();
            instance = null;
        }
    }

    public void Show()
    {
        if (instance != null && !instance.IsDisposed)
        {
            instance.Activate();
        }
        else
        {
            instance = new Window(config);
            instance.Show();
        }
    }

    private sealed class Window : Form
    {
        private CompositeDisposable disposable = new CompositeDisposable();
        private IConfig config;

        public Window(IConfig config)
        {
            this.config = config;

            this.Text = "About";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

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
                    x.Text = $"{config.AppFileInfo.ProductName}";
                    x.Font = new Font(Font.FontFamily, 20);
                    x.AutoSize = true;
                    x.Margin = new Padding(0, 3, 0, 3);
                }).DisposeWith(disposable)
                .Add<TableLayoutPanel, Label>(1, 1, x =>
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"Version: {config.AppFileInfo.ProductVersion}");
                    sb.AppendLine($"{config.AppFileInfo.LegalCopyright}");
                    sb.AppendLine();
                    sb.AppendLine($"{config.AppFileInfo.Comments}");
                    sb.AppendLine();
                    sb.AppendLine($"Runtime: {Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName}");

                    x.Text = sb.ToString();
                    x.AutoSize = true;
                    x.Margin = new Padding(5, 15, 20, 3);
                }).DisposeWith(disposable)
                .Add<TableLayoutPanel, TableLayoutPanel>(0, 2, 2, 1, x =>
                {
                    x.Dock = DockStyle.Fill;
                    x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                    x.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                    x.Add<TableLayoutPanel, CheckBox>(0, 0, y =>
                    {
                        y.AutoSize = true;
                        y.Margin = new Padding(20, 3, 0, 20);
                        y.Text = "Run at startup";
                        y.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
                        y.DataBindings.Add(
                            "Checked",
                            config.UserConfig,
                            nameof(UserConfig.RunAtStartup),
                            false,
                            DataSourceUpdateMode.OnPropertyChanged);
                    }).DisposeWith(disposable)
                    .Add<TableLayoutPanel, Button>(1, 0, y =>
                    {
                        y.Text = "OK";
                        y.AutoSize = true;
                        y.Padding = new Padding(15, 3, 15, 3);
                        y.Margin = new Padding(0, 0, 20, 20);
                        y.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                        y.Click += (sender, e) => Close();
                    }).DisposeWith(disposable);
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
    }
}