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
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reactive.Disposables;
using System.Linq;
using flowOSD.Api;
using System.Reactive.Linq;
using static flowOSD.Native;

namespace flowOSD.UI.Components;

sealed class CxContextMenu : ContextMenuStrip
{
    private CompositeDisposable disposable;
    private ISystemEvents systemEvents;

    public CxContextMenu(ISystemEvents systemEvents)
    {
        this.systemEvents = systemEvents;

        disposable = new CompositeDisposable();

        Renderer = new MenuRenderer(systemEvents).DisposeWith(disposable);
        BackColor = Color.Transparent;

        SetCornerPreference(Handle, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);
        UpdateMode();

        this.systemEvents.SystemDarkMode
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(isDarkMode => UpdateMode(isDarkMode))
            .DisposeWith(disposable);
    }

    public static void EnableAcrylic(IWin32Window window, bool isDarkMode)
    {
        var color = isDarkMode
            ? Color.FromArgb(210, 44, 44, 44)
            : Color.FromArgb(210, 249, 249, 249);

        Native.EnableAcrylic(window, color);
    }

    public static ToolStripMenuItem CreateMenuItem(CommandBase command, object commandParameter = null)
    {
        var item = new ToolStripMenuItem();
        item.Margin = new Padding(0, 8, 0, 8);
        item.Command = command;
        item.CommandParameter = commandParameter;

        item.DataBindings.Add("Text", command, "Text");
        item.DataBindings.Add("Visible", command, "Enabled");

        return item;
    }

    public static ToolStripSeparator CreateSeparator(ToolStripItem dependsOn = null)
    {
        var item = new ToolStripSeparator();

        if (dependsOn != null)
        {
            dependsOn.VisibleChanged += (sender, e) => item.Visible = dependsOn.Visible;
        }

        return item;
    }

    public ToolStripItem AddMonitoringItem(string text)
    {
        var item = new ToolStripLabel();
        item.Margin = new Padding(0);
        item.Text = text;
        item.Enabled = false;

        Items.Add(item);

        return item;
    }

    public ToolStripItem AddMenuItem(CommandBase command, object commandParameter = null)
    {
        var item = CreateMenuItem(command, commandParameter);

        Items.Add(item);

        return item;
    }

    public ToolStripSeparator AddSeparator(ToolStripItem dependsOn = null)
    {
        var item = CreateSeparator(dependsOn);

        Items.Add(item);

        return item;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            disposable.Dispose();
            disposable = null;
        }

        base.Dispose(disposing);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Color.Transparent);
    }

    private async void UpdateMode(bool? isDarkMode = null)
    {
        var isDark = isDarkMode ?? await systemEvents.SystemDarkMode.FirstAsync();

        EnableAcrylic(this, isDark);
    }

    private class MenuRenderer : ToolStripRenderer, IDisposable
    {
        private Brush selectedBrush, textBrush, selectedTextBrush;
        private Pen separatorPen;
        private CompositeDisposable disposable;

        private ISystemEvents systemEvents;

        public MenuRenderer(ISystemEvents systemEvents)
        {
            this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));

            disposable = new CompositeDisposable();

            separatorPen = new Pen(Color.FromArgb(255, 96, 96, 96), 1).DisposeWith(disposable);
            selectedBrush = new SolidBrush(Color.FromArgb(255, 25, 110, 191)).DisposeWith(disposable);

            UpdateMode();

            this.systemEvents.SystemDarkMode
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(isDarkMode => UpdateMode(isDarkMode))
                .DisposeWith(disposable);
        }

        public void Dispose()
        {
            disposable?.Dispose();
            disposable = null;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.Graphics.TextRenderingHint = e.TextFont.Height < 25 ?
                TextRenderingHint.ClearTypeGridFit: TextRenderingHint.AntiAliasGridFit;

            if (e.Item is ToolStripMenuItem)
            {
                var textHeight = e.TextFont.GetHeight(e.Graphics);
                var point = new PointF(
                    e.TextRectangle.X,
                    e.TextRectangle.Y + (e.TextRectangle.Height - textHeight) / 2);

                e.Graphics.DrawString(
                    e.Text,
                    e.TextFont,
                    e.Item.Selected ? selectedTextBrush : textBrush,
                    point);
            }
            else
            {
                var t = e.Item.Text.Split(":");
                var labelText = t[0] + ": ";
                var labelValue = t[1];

                using var font = new Font(
                    "Segoe UI",
                    e.TextFont.Size * 0.8f,
                    GraphicsUnit.Pixel);

                var textSize = e.Graphics.MeasureString(labelText, font);
                var point = new PointF(
                    e.TextRectangle.X,
                    e.TextRectangle.Y + (e.TextRectangle.Height - textSize.Height) / 2);

                e.Graphics.DrawString(
                    labelText,
                    font,
                    Brushes.Gray,
                    point);

                point.X += textSize.Width;

                e.Graphics.DrawString(
                    labelValue,
                    font,
                    textBrush,
                    point);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var y = e.Item.ContentRectangle.Y + e.Item.ContentRectangle.Height / 2;

            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
            e.Graphics.DrawLine(
                separatorPen,
                e.Item.ContentRectangle.X,
                y,
                e.Item.ContentRectangle.Right,
                y);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                var x = e.Item.ContentRectangle.X + 4;
                var y = e.Item.ContentRectangle.Y + 1;
                var width = e.Item.ContentRectangle.Width - 8;
                var height = e.Item.ContentRectangle.Height - 2;

                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
                e.Graphics.FillRoundedRectangle(selectedBrush, x, y, width, height, 4);
            }
        }

        private async void UpdateMode(bool? isDarkMode = null)
        {
            var isDark = isDarkMode ?? await systemEvents.SystemDarkMode.FirstAsync();

            textBrush = isDark ? Brushes.White : Brushes.Black;
            selectedTextBrush = Brushes.White;
        }
    }
}
