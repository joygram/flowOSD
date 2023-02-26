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
namespace flowOSD.UI;

using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;
using flowOSD.Api;
using flowOSD.UI.Components;
using static Extensions;
using static Native;

sealed class MainUI : IDisposable
{
    private Window form;
    private IConfig config;
    private ISystemEvents systemEvents;


    public MainUI(IConfig config, ISystemEvents systemEvents)
    {
        this.config = config;
        this.systemEvents = systemEvents;

        form = new Window(this);
        form.Visible = false;
    }

    void IDisposable.Dispose()
    {
        if (form != null && !form.IsDisposed)
        {
            form.Dispose();
            form = null;
        }
    }

    public void Show()
    {
        form.Show();
        form.Activate();
    }

    private sealed class Window : Form
    {
        private CxTabListener tabListener = new CxTabListener();
        private MainUI owner;

        public Window(MainUI owner)
        {
            this.owner = owner;

            FormBorderStyle = FormBorderStyle.None;

            ShowInTaskbar = false;
            DoubleBuffered = true;
            TopMost = true;

            KeyPreview = true;

            var xx = Create<TableLayoutPanel>(x =>
            {
                x.BackColor = Color.Transparent;
                x.Dock = DockStyle.Fill;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                x.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                x.Padding = new Padding(0);
            });

            xx.Add<TableLayoutPanel>(0, 0, x =>
            {
                x.Margin = new Padding(20, 20, 20, 10);

                x.Dock = DockStyle.None;
                x.AutoSize = true;
                x.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3F));

                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                x.Add<CxButton>(0, 0, x => FillButton(x, "\ue945"));
                x.Add<CxButton>(1, 0, x => FillButton(x, "\ue7f4"));
                x.Add<CxButton>(2, 0, x => FillButton(x, "\ue950"));
                x.Add<CxButton>(0, 2, x => FillButton(x, "\uefa5"));
                x.Add<CxButton>(1, 2, x =>
                {
                    FillButton(x, "\uec49");
                    x.SystemEvents = owner.systemEvents;
                    x.DropDownItems = new string[] { "Silent", "Balanced", "Turbo" };
                });
                x.Add<CxButton>(2, 2, x =>
                {
                    FillButton(x, "\ue9d9");
                    x.IsToggle = false;
                });

                x.Add<CxLabel>(0, 1, x => FillLabel(x, "CPU Boost"));
                x.Add<CxLabel>(1, 1, x => FillLabel(x, "High Refesh Rate"));
                x.Add<CxLabel>(2, 1, x => FillLabel(x, "eGPU"));
                x.Add<CxLabel>(0, 3, x => FillLabel(x, "Touchpad"));
                x.Add<CxLabel>(1, 3, x => FillLabel(x, "Balanced"));
                x.Add<CxLabel>(2, 3, x => FillLabel(x, "Fan Profile"));

            });

            xx.Add<TableLayoutPanel>(0, 1, x =>
            {
                x.Margin = new Padding(5);

                x.Dock = DockStyle.Fill;
                x.AutoSize = true;
                x.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                x.Add<CxButton>(0, 0, button =>
                {
                    button.Margin = new Padding(5);

                    button.Size = new Size(128, 64);
                    button.Font = new Font("Segoe UI Light", 16, FontStyle.Bold, GraphicsUnit.Pixel);
                    button.Text = "-12.3 W";
                    button.Symbol = "\ue83e";
                    button.SymbolHeight = 26;
                    button.ForeColor = Color.White;
                    button.IsToggle = false;
                    button.IsTransparent = true;
                    button.BackColor = Color.FromArgb(255, 60, 60, 60);

                    button.TabListener = tabListener;
                });

                x.Add<CxButton>(1, 0, button =>
                {
                    button.Margin = new Padding(5);

                    button.Size = new Size(64, 64);
                    button.Symbol = "\ue713";
                    button.SymbolHeight = 24;
                    button.ForeColor = Color.White;
                    button.IsToggle = false;
                    button.IsTransparent = true;
                    button.BackColor = Color.FromArgb(255, 60, 60, 60);

                    button.TabListener = tabListener;
                });

                x.Paint += X_Paint;

            });

            Controls.Add(xx);

            //var tb = new CxButton
            //{
            //    Location = new Point(50, 50),
            //    //Size = new Size(140, 70),
            //    Size = new Size(64, 64),
            //    Font = new Font("Segoe Fluent Icons", 25, GraphicsUnit.Pixel),
            //    Text = "\ue713",
            //    //Text = "\ue945",
            //    ForeColor = Color.White,
            //    IsToggle = false,
            //    IsTransparent = true,
            //    BackColor = Color.FromArgb(255, 60, 60, 60),

            //    TabListener = tabListener
            //};
            //Controls.Add(tb);

            /*  var tb2 = new CxButton
              {
                  Location = new Point(30, 30),
                  Size = new Size(140, 70),
                  Font = new Font("Segoe Fluent Icons", 25, GraphicsUnit.Pixel),
                  Text = "\ue945",
                  ForeColor = Color.White,
                  IsToggle = true,
                  IsTransparent = false,
                  BackColor = Color.FromArgb(255, 70, 70, 70),
                  AccentColor = Color.FromArgb(255, 0, 145, 248),

                  TabListener = tabListener
              };

              var tb3 = new CxButton
              {
                  Location = new Point(30+140+20, 30),
                  Size = new Size(140, 70),
                  Font = new Font("Segoe Fluent Icons", 25, GraphicsUnit.Pixel),
                  Text = "\ue7f4",
                  ForeColor = Color.White,
                  IsToggle = true,
                  IsTransparent = false,
                  BackColor = Color.FromArgb(255, 70, 70, 70),
                  AccentColor = Color.FromArgb(255, 0, 145, 248),

                  TabListener = tabListener
              };

              var tb4 = new CxButton
              {
                  Location = new Point(30+140+20+140+20, 30),
                  Size = new Size(140, 70),
                  Font = new Font("Segoe Fluent Icons", 25, GraphicsUnit.Pixel),
                  Text = "\ue950",
                  ForeColor = Color.White,
                  IsToggle = true,
                  IsTransparent = false,
                  BackColor = Color.FromArgb(255, 70, 70, 70),
                  AccentColor = Color.FromArgb(255, 0, 145, 248),

                  TabListener = tabListener
              };

              Controls.Add(tb2);
              Controls.Add(tb3);
              Controls.Add(tb4);*/

            UpdateTheme();
        }

        private void X_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.Black, e.ClipRectangle);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            SetCornerPreference(Handle, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);

            var dX = 20;
            var dY = 20;

            Width = 540;
            Height = 400;

            Left = Screen.PrimaryScreen.WorkingArea.Width - Width - dX;
            Top = Screen.PrimaryScreen.WorkingArea.Height - Height - dY;

            base.OnHandleCreated(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Transparent);
        }

        protected override void OnDeactivate(EventArgs e)
        {
            Hide();

            base.OnDeactivate(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == 27)
            {
                Hide();
            }

            base.OnKeyPress(e);
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible)
            {
                tabListener.ShowKeyboardFocus = false;
            }

            base.OnVisibleChanged(e);
        }

        private void UpdateTheme()
        {
            var color = true
                ? Color.FromArgb(210, 44, 44, 44)
                : Color.FromArgb(210, 249, 249, 249);

            EnableAcrylic(this, color);

            Invalidate();
        }

        private void FillLabel(Label x, string v)
        {
            x.AutoSize = true;
            x.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            x.Margin = new Padding(0, 0, 0, 20);
            x.TextAlign = ContentAlignment.MiddleCenter;
            x.ForeColor = Color.White;
            x.Dock = DockStyle.None;
            x.Text = v;

            x.Font = new Font(
         "Segoe UI Light",
         16,
         GraphicsUnit.Pixel);

        }

        private void FillButton(CxButton button, string symbol, string text = null)
        {
            button.Margin = new Padding(15, 10, 15, 10);

            button.Size = new Size(140, 70);
            button.Font = new Font("Segoe UI Light", 26, FontStyle.Bold, GraphicsUnit.Pixel);
            button.Symbol = symbol;
            button.SymbolHeight = 26;
            button.Text = text;
            button.ForeColor = Color.White;
            button.IsToggle = true;
            button.IsTransparent = false;
            button.BackColor = Color.FromArgb(255, 60, 60, 60);

            button.TabListener = tabListener;
        }
    }
}
