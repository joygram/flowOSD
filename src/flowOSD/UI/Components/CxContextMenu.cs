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
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reactive.Disposables;
using System.Linq;
using flowOSD.Api;
using System.Reactive.Linq;
using static flowOSD.Native;
using System.Windows.Input;

namespace flowOSD.UI.Components;

sealed class CxContextMenu : ContextMenuStrip
{
    private CompositeDisposable disposable;

    public CxContextMenu()
    {
        disposable = new CompositeDisposable();

        base.Renderer = new MenuRenderer().DisposeWith(disposable);

        BackgroundHoverColor = Color.FromArgb(255, 25, 110, 191);
        SeparatorColor = Color.FromArgb(255, 96, 96, 96);
        TextColor = Color.White;
        TextBrightColor = Color.Black;

        SetCornerPreference(Handle, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);
    }

    public Color BackgroundColor
    {
        get => Renderer.BackgroundColor;
        set
        {
            if (Renderer.BackgroundColor == value)
            {
                return;
            }

            Renderer.BackgroundColor = value;
            EnableAcrylic(this, Renderer.BackgroundColor.SetAlpha(210));
        }
    }

    public Color BackgroundHoverColor
    {
        get => Renderer.BackgroundHoverColor;
        set
        {
            if (Renderer.BackgroundHoverColor == value)
            {
                return;
            }

            Renderer.BackgroundHoverColor = value;
            Invalidate();
        }
    }

    public Color SeparatorColor
    {
        get => Renderer.SeparatorColor;
        set
        {
            if (Renderer.SeparatorColor == value)
            {
                return;
            }

            Renderer.SeparatorColor = value;
            Invalidate();
        }
    }

    public Color TextColor
    {
        get => Renderer.TextColor;
        set
        {
            if (Renderer.TextColor == value)
            {
                return;
            }

            Renderer.TextColor = value;
            Invalidate();
        }
    }

    public Color TextBrightColor
    {
        get => Renderer.TextBrightColor;
        set
        {
            if (Renderer.TextBrightColor == value)
            {
                return;
            }

            Renderer.TextBrightColor = value;
            Invalidate();
        }
    }

    private new MenuRenderer Renderer => base.Renderer as MenuRenderer;

    public static ToolStripMenuItem CreateMenuItem(string text, ICommand command, object commandParameter = null)
    {
        var item = new ToolStripMenuItem();
        item.Margin = new Padding(0, 8, 0, 8);
        item.Text = text;
        item.Command = command;
        item.CommandParameter = commandParameter;

        return item;
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

    public ToolStripItem AddMenuItem(string text, ICommand command, object commandParameter = null)
    {
        var item = CreateMenuItem(text, command, commandParameter);

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

    private class MenuRenderer : ToolStripRenderer, IDisposable
    {
        private SolidBrush textBrush, textBrightBrush, backgroundHoverBrush;
        private Pen separatorPen;

        private CompositeDisposable disposable;

        public MenuRenderer()
        {
            disposable = new CompositeDisposable();
        }

        void IDisposable.Dispose()
        {
            disposable?.Dispose();
            disposable = null;
        }

        public Color BackgroundColor
        {
            get; set;
        }

        public Color BackgroundHoverColor
        {
            get => (backgroundHoverBrush?.Color)??Color.Empty;
            set
            {
                if (backgroundHoverBrush?.Color == value)
                {
                    return;
                }

                if (backgroundHoverBrush != null)
                {
                    disposable.Remove(backgroundHoverBrush);
                    backgroundHoverBrush.Dispose();
                }

                backgroundHoverBrush = new SolidBrush(value).DisposeWith(disposable);
            }
        }

        public Color SeparatorColor
        {
            get => (separatorPen?.Color) ?? Color.Empty;
            set
            {
                if (separatorPen?.Color == value)
                {
                    return;
                }

                if (separatorPen != null)
                {
                    disposable.Remove(separatorPen);
                    separatorPen.Dispose();
                }

                separatorPen = new Pen(value, 1).DisposeWith(disposable);
            }
        }

        public Color TextColor
        {
            get => (textBrush?.Color) ?? Color.Empty;
            set
            {
                if (textBrush?.Color == value)
                {
                    return;
                }

                if (textBrush != null)
                {
                    disposable.Remove(textBrush);
                    textBrush.Dispose();
                }

                textBrush = new SolidBrush(value).DisposeWith(disposable);
            }
        }

        public Color TextBrightColor
        {
            get => (textBrightBrush?.Color) ?? Color.Empty;
            set
            {
                if (textBrightBrush?.Color == value)
                {
                    return;
                }

                if (textBrightBrush != null)
                {
                    disposable.Remove(textBrightBrush);
                    textBrightBrush.Dispose();
                }

                textBrightBrush = new SolidBrush(value).DisposeWith(disposable);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.Graphics.TextRenderingHint =  TextRenderingHint.AntiAliasGridFit;

            if (e.Item is ToolStripMenuItem)
            {
                var textHeight = e.TextFont.GetHeight(e.Graphics);
                var point = new PointF(
                    e.TextRectangle.X,
                    e.TextRectangle.Y + (e.TextRectangle.Height - textHeight) / 2);

                var backgroundColor = e.Item.Selected ? BackgroundHoverColor : BackgroundColor;

                e.Graphics.DrawString(
                    e.Text,
                    e.TextFont,
                    backgroundColor.IsBright() ? textBrightBrush : textBrush,
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
                e.Graphics.FillRoundedRectangle(backgroundHoverBrush, x, y, width, height, 4);
            }
        }
    }
}
