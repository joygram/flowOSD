/*  Copyright © 2021-2022, Albert Akhmetov <akhmetov@live.com>   
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

using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using flowOSD.Api;
using static Extensions;
using static Native;

sealed class ConfigUI : IDisposable
{
    private Window instance;
    private IConfig config;
    private ICommandManager commandManager;

    public ConfigUI(IConfig config, ICommandManager commandManager)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));
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
            instance = new Window(
                new ConfigPages.GeneralConfigPage(config),
                new ConfigPages.NotificationsConfigPage(config),
                new ConfigPages.HotKeysConfigPage(config, commandManager));
            instance.Show();
        }
    }

    private sealed class Window : Form
    {
        private CompositeDisposable disposable = new CompositeDisposable();
        private ReadOnlyCollection<Control> pages;
        private Control currentPage;

        private TableLayoutPanel layout;

        public Window(params Control[] pages)
        {
            this.pages = new ReadOnlyCollection<Control>(pages);

            Init();

            CurrentPage = pages.FirstOrDefault();
        }

        public Control CurrentPage
        {
            get => currentPage;
            set
            {
                if (currentPage != null)
                {
                    layout.Controls.Remove(currentPage);
                }

                currentPage = value;
                currentPage.Dock = DockStyle.Fill;

                if (currentPage != null)
                {
                    layout.Add(1, 0, currentPage);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            disposable?.Dispose();
            disposable = null;

            base.OnClosed(e);
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

        private void UpdateSize(int dpi)
        {
            var scale = dpi / 96f;
            this.Size = new Size((int)(600 * scale), (int)(500 * scale));
        }

        private void Init()
        {
            const int listWidth = 150;
            const int listItemHeight = 30;

            layout = Create<TableLayoutPanel>(x =>
            {
                x.Dock = DockStyle.Fill;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                x.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize, 100));
            }).DisposeWith(disposable);

            layout.Add<ListBox>(0, 0, x =>
            {
                var scale = GetDpiForWindow(Handle) / 96f;
                x.Width = (int)(listWidth * scale);

                x.DrawMode = DrawMode.OwnerDrawVariable;
                x.IntegralHeight = false;
                x.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;

                x.DataSource = pages;
                x.DisplayMember = nameof(Control.Text);
             
                x.SelectedIndexChanged += (_, _) =>
                {
                    CurrentPage = x.SelectedIndex < 0 ? null : pages[x.SelectedIndex];
                };

                x.DrawItem += (_, e) =>
                {
                    var text = pages[e.Index].Text;
                    var textSize = e.Graphics.MeasureString(text, e.Font);

                    e.DrawBackground();
                    e.DrawFocusRectangle();

                    using var brush = new SolidBrush(e.State == DrawItemState.Selected ? e.BackColor : e.ForeColor);

                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    e.Graphics.DrawString(
                        text,
                        e.Font,
                        brush,
                        e.Bounds.Left + (e.Bounds.Height - textSize.Height) / 2,
                        e.Bounds.Top + (e.Bounds.Height - textSize.Height) / 2);
                };

                x.MeasureItem += (_, e) =>
                {
                    var scale = GetDpiForWindow(Handle) / 96f;

                    e.ItemHeight = (int)(listItemHeight * scale);
                    e.ItemWidth = (int)(listWidth * scale);
                };

                x.DpiChangedAfterParent += (_, _) =>
                {
                    var scale = GetDpiForWindow(Handle) / 96f;
                    x.Width = (int)(listWidth * scale);
                };

                x.DisposeWith(disposable);
            });

            layout.Add<Label>(0, 1, 2, 1, x =>
            {
                x.Dock = DockStyle.Fill;
                x.Margin = new Padding(5, 10, 5, 10);
                x.AutoSize = false;

                x.Height = 2;
                x.BorderStyle = BorderStyle.Fixed3D;

                x.DisposeWith(disposable);
            });

            layout.Add<Button>(1, 2, x =>
            {
                x.Text = "Close";
                x.AutoSize = true;
                x.Padding = new Padding(15, 3, 15, 3);
                x.Margin = new Padding(5);
                x.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                x.Click += (sender, e) => Close();

                x.DisposeWith(disposable);
            });

            this.Add(layout);

            Padding = new Padding(10);
            DoubleBuffered = true;

            Text = "Settings";
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;

            var scale = GetDpiForWindow(Handle) / 96f;
            this.Font = new Font("Segoe UI", 12 * scale, GraphicsUnit.Pixel);
            UpdateSize(GetDpiForWindow(Handle));            

            StartPosition = FormStartPosition.CenterScreen;
        }
    }
}