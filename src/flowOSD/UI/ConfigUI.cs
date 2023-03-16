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
namespace flowOSD.UI;

using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using flowOSD.Api;
using flowOSD.Extensions;
using static flowOSD.Extensions.Forms;

sealed class ConfigUI : IDisposable
{
    private Window? instance;
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
                new ConfigPages.HotKeysConfigPage(config, commandManager),
                new ConfigPages.MonitoringConfigPage(config));
            instance.Show();
        }
    }

    private sealed class Window : Form
    {
        private CompositeDisposable? disposable = new CompositeDisposable();

        private ReadOnlyCollection<Control> pages;
        private Control? currentPage;

        private TableLayoutPanel layout;
        private Panel pageContainer;

        public Window(params Control[] pages)
        {
            this.pages = new ReadOnlyCollection<Control>(pages);

            layout = Init(disposable);

            CurrentPage = pages.FirstOrDefault();
        }

        public Control? CurrentPage
        {
            get => currentPage;
            set
            {
                if (currentPage != null)
                {
                    pageContainer.Controls.Remove(currentPage);
                }

                currentPage = value;

                if (currentPage != null)
                {
                    pageContainer.Controls.Add(currentPage);
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
            UpdateSize();

            base.OnShown(e);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            UpdateSize();

            base.OnDpiChanged(e);
        }

        private void UpdateSize()
        {
            Size = this.DpiScale(new Size(600, 500));
        }

        private TableLayoutPanel Init(CompositeDisposable uiDisposable)
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
            }).DisposeWith(uiDisposable);

            layout.Add<Panel>(1, 0, x =>
            {
                x.Dock = DockStyle.Fill;
                x.AutoScroll = true;
                x.AutoSize = false;

                x.LinkAs(ref pageContainer);
            });

            layout.Add<ListBox>(0, 0, x =>
            {
                x.Width = this.DpiScale(listWidth);

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
                    if (e.Font == null)
                    {
                        return;
                    }

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
                    e.ItemHeight = this.DpiScale(listItemHeight);
                    e.ItemWidth = this.DpiScale(listWidth);
                };

                x.DpiChangedAfterParent += (_, _) =>
                {
                    x.Width = this.DpiScale(listWidth);
                };

                x.DisposeWith(uiDisposable);
            });

            layout.Add<Label>(0, 1, 2, 1, x =>
            {
                x.Dock = DockStyle.Fill;
                x.Margin = new Padding(5, 10, 5, 10);
                x.AutoSize = false;

                x.Height = 2;
                x.BorderStyle = BorderStyle.Fixed3D;

                x.DisposeWith(uiDisposable);
            });

            layout.Add<Button>(1, 2, x =>
            {
                x.Text = "Close";
                x.AutoSize = true;
                x.Padding = new Padding(15, 3, 15, 3);
                x.Margin = new Padding(5);
                x.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                x.Click += (sender, e) => Close();

                x.DisposeWith(uiDisposable);
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

            Font = new Font("Segoe UI", this.DpiScale(12), GraphicsUnit.Pixel);
            UpdateSize();

            StartPosition = FormStartPosition.CenterScreen;

            return layout;
        }
    }
}