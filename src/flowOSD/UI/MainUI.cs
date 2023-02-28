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
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;
using flowOSD.Api;
using flowOSD.UI.Commands;
using flowOSD.UI.Components;
using static Extensions;
using static Native;

sealed class MainUI : IDisposable
{
    private Window form;
    private IConfig config;
    private ISystemEvents systemEvents;
    private ICommandManager commandManager;


    public MainUI(IConfig config, ISystemEvents systemEvents, ICommandManager commandManager)
    {
        this.config = config;
        this.systemEvents = systemEvents;
        this.systemEvents.Dpi.Subscribe(x => { form?.Dispose(); form = null; });

        this.commandManager = commandManager;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

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
        if (form == null)
        {
            form = new Window(this);
            form.Visible = false;
        }

        var offset = form.DpiScale(14);

        form.Width = form.DpiScale(350);
        form.Height = form.DpiScale(300);

        form.Left = Screen.PrimaryScreen.WorkingArea.Width - form.Width - offset;
        form.Top = Screen.PrimaryScreen.WorkingArea.Height - form.Height - offset;
        form.Show();

        form.Activate();
    }

    private sealed class Window : Form
    {
        private CompositeDisposable disposable = new CompositeDisposable();

        private CxTabListener tabListener = new CxTabListener();
        private IList<CxButton> buttonList = new List<CxButton>();
        private IList<CxLabel> labelList = new List<CxLabel>();

        private MainUI owner;

        private CxButton boostButton, refreshRateButton, eGpuButton, touchpadButton;
        private CxLabel boostLabel, refreshRateLabel, eGpuLabel, touchpadLabel;

        public Window(MainUI owner)
        {
            this.owner = owner;
            this.owner.systemEvents.SystemUI
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => UpdateTheme(x))
                .DisposeWith(disposable);

            FormBorderStyle = FormBorderStyle.None;

            ShowInTaskbar = false;
            DoubleBuffered = true;
            TopMost = true;

            KeyPreview = true;

            InitComponents();
        }

        private void InitComponents()
        {
            var layout = Create<TableLayoutPanel>(x =>
            {
                x.BackColor = Color.Transparent;
                x.Dock = DockStyle.Fill;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                x.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                x.Padding = new Padding(0);
            });

            layout.Add<TableLayoutPanel>(0, 0, x =>
            {
                x.Margin = this.DpiScale(new Padding(14, 14, 14, 14));

                x.Dock = DockStyle.None;
                x.AutoSize = true;
                x.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / 3f));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / 3f));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 / 3f));

                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                var iconFont = new Font(UIParameters.IconFontName, this.DpiScale(16), GraphicsUnit.Pixel).DisposeWith(disposable);

                boostButton = CreateButton(iconFont, 
                    "\ue945",
                    command: owner.commandManager.Resolve<ToggleBoostCommand>())
                .To(ref buttonList).DisposeWith(disposable);

                refreshRateButton = CreateButton(
                    iconFont, 
                    "\ue7f4",
                    command: owner.commandManager.Resolve<ToggleRefreshRateCommand>())
                .To(ref buttonList).DisposeWith(disposable);
                
                eGpuButton = CreateButton(
                    iconFont, 
                    "\ue950",
                    command: owner.commandManager.Resolve<ToggleGpuCommand>())
                .To(ref buttonList).DisposeWith(disposable);
               
                touchpadButton = CreateButton(
                    iconFont, 
                    "\uefa5",
                    command: owner.commandManager.Resolve<ToggleTouchPadCommand>())
                .To(ref buttonList).DisposeWith(disposable);

                x.Add(0, 0, boostButton);
                x.Add(1, 0, refreshRateButton);
                x.Add(2, 0, eGpuButton);
                x.Add(0, 2, touchpadButton);

                var textFont = new Font(UIParameters.FontName, this.DpiScale(10), GraphicsUnit.Pixel).DisposeWith(disposable);

                boostLabel = CreateLabel(textFont, "CPU Boost").To(ref labelList).DisposeWith(disposable);
                refreshRateLabel = CreateLabel(textFont, "High Refesh Rate").To(ref labelList).DisposeWith(disposable);
                eGpuLabel = CreateLabel(textFont, "eGPU").To(ref labelList).DisposeWith(disposable);
                touchpadLabel = CreateLabel(textFont, "Touchpad").To(ref labelList).DisposeWith(disposable);

                x.Add(0, 1, boostLabel);
                x.Add(1, 1, refreshRateLabel);
                x.Add(2, 1, eGpuLabel);
                x.Add(0, 3, touchpadLabel);

            });

            layout.Add<TableLayoutPanel>(0, 1, x =>
            {
                x.Margin = this.DpiScale(new Padding(3));

                x.Dock = DockStyle.Fill;
                x.AutoSize = true;
                x.AutoSizeMode = AutoSizeMode.GrowAndShrink;

                x.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                x.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

                x.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                x.Add<CxButton>(0, 0, button =>
                {
                    button.Margin = this.DpiScale(new Padding(3));

                    button.Size = this.DpiScale(new Size(80, 40));
                    button.Font = new Font(UIParameters.FontName, this.DpiScale(10), GraphicsUnit.Pixel).DisposeWith(disposable);
                    button.Text = "-12.3 W";
                    button.Icon = "\ue83e";
                    button.IconFont = new Font(UIParameters.IconFontName, this.DpiScale(18), GraphicsUnit.Pixel).DisposeWith(disposable);
                    button.IsToggle = false;
                    button.IsTransparent = true;
                    button.TabListener = tabListener;

                    button.To(ref buttonList);
                    button.DisposeWith(disposable);
                });

                x.Add<CxButton>(1, 0, button =>
                {
                    button.Margin = this.DpiScale(new Padding(3));

                    button.Size = this.DpiScale(new Size(40, 40));
                    button.Icon = "\ue713";
                    button.IconFont = new Font(UIParameters.IconFontName, this.DpiScale(17), GraphicsUnit.Pixel).DisposeWith(disposable);
                    button.IsToggle = false;
                    button.IsTransparent = true;
                    button.TabListener = tabListener;

                    button.To(ref buttonList);
                    button.DisposeWith(disposable);

                    button.Command = owner.commandManager.Resolve<SettingsCommand>();
                });
            });

            Controls.Add(layout);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                disposable?.Dispose();
                disposable = null;
            }

            base.Dispose(disposing);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            SetCornerPreference(Handle, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);

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

        private CxLabel CreateLabel(Font textFont, string text = null)
        {
            var x = new CxLabel();

            x.AutoSize = true;
            x.Margin = this.DpiScale(new Padding(0, 0, 0, 14));
            x.TextAlign = ContentAlignment.MiddleCenter;
            x.Dock = DockStyle.Fill;
            x.Text = text;
            x.Font = textFont;

            return x;
        }

        private CxButton CreateButton(Font iconFont, string icon, Font textFont = null, string text = null, CommandBase command = null)
        {
            var x = new CxButton();

            x.Margin = this.DpiScale(new Padding(10, 8, 10, 8));
            x.Size = this.DpiScale(new Size(100, 50));
            x.Icon = icon;
            x.IconFont = iconFont;
            x.Text = text;
            x.Font = textFont;
            x.IsToggle = true;
            x.IsTransparent = false;
            x.TabListener = tabListener;

            if (command != null)
            {
                x.Command = command;
                x.DataBindings.Add("IsChecked", command, "IsChecked");
            }

            return x;
        }

        private void UpdateTheme(UIParameters parameters)
        {
            foreach (var button in buttonList)
            {
                button.AccentColor = parameters.AccentColor;
                button.FocusColor = parameters.FocusColor;

                button.BackColor = parameters.ButtonBackgroundColor;
                button.TextColor = parameters.ButtonTextColor;
                button.TextBrightColor = parameters.ButtonTextBrightColor;
            }

            foreach (var label in labelList)
            {
                label.ForeColor = parameters.TextColor;
            }

            EnableAcrylic(this, parameters.BackgroundColor.SetAlpha(210));

            Invalidate();
        }
    }
}
