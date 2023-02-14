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

using System;
using System.Drawing.Drawing2D;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using flowOSD.Api;
using flowOSD.Services;
using flowOSD.UI.Commands;
using static Extensions;
using static Native;

sealed class TrayIcon : IDisposable
{
    private CompositeDisposable disposable = new CompositeDisposable();
    private NotifyIcon notifyIcon;
    private ToolStripMenuItem highRefreshRateMenuItem, touchPadMenuItem, boostMenuItem, aboutMenuItem;

    private NativeUI nativeUI;
    private ICommandManager commandManager;
    private IConfig config;
    private IImageSource imageSource;
    private ISystemEvents systemEvents;

    public TrayIcon(NativeUI nativeUI, IConfig config, IImageSource imageSource, ICommandManager commandManager, ISystemEvents systemEvents)
    {
        this.nativeUI = nativeUI ?? throw new ArgumentNullException(nameof(nativeUI));

        this.config = config ?? throw new ArgumentNullException(nameof(config));
        this.imageSource = imageSource ?? throw new ArgumentNullException(nameof(imageSource));
        this.systemEvents = systemEvents ?? throw new ArgumentNullException(nameof(systemEvents));
        this.commandManager = commandManager ?? throw new ArgumentNullException(nameof(commandManager));

        Init();

        BindCommandManager(commandManager);

        nativeUI.Dpi
            .Subscribe(dpi => UpdateDpi(dpi))
            .DisposeWith(disposable);

        systemEvents.TabletMode
            .CombineLatest(systemEvents.SystemDarkMode, nativeUI.Dpi.Throttle(TimeSpan.FromSeconds(2)), (isTabletMode, isDarkMode, dpi) => new { isTabletMode, isDarkMode, dpi })
            .Throttle(TimeSpan.FromMilliseconds(100))
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(x => UpdateNotifyIcon(x.isTabletMode, x.isDarkMode, x.dpi))
            .DisposeWith(disposable);
    }

    void IDisposable.Dispose()
    {
        disposable?.Dispose();
        disposable = null;
    }

    private void BindCommandManager(ICommandManager commandManager)
    {
        foreach (var item in notifyIcon.ContextMenuStrip.Items)
        {
            if (item is CommandMenuItem commandItem)
            {
                commandItem.CommandManager = commandManager;
            }
        }
    }

    private void Init()
    {
        notifyIcon = Create<NotifyIcon>().DisposeWith(disposable);
        notifyIcon.Click += (sender, e) => ShowMenu();

        notifyIcon.Text = $"{config.AppFileInfo.ProductName} ({config.AppFileInfo.ProductVersion})";

#if DEBUG
        notifyIcon.Text += " [DEBUG BUILD]";
#endif

        notifyIcon.ContextMenuStrip = InitContextMenu();


        notifyIcon.Visible = true;
    }

    private void ShowMenu()
    {
        var methodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        methodInfo.Invoke(notifyIcon, null);
    }

    private ContextMenuStrip InitContextMenu()
    {
        var menu = Create<Menu>().Add(
            CreateCommandMenuItem(nameof(ToggleRefreshRateCommand)).DisposeWith(disposable).LinkAs(ref highRefreshRateMenuItem),
            CreateSeparator(highRefreshRateMenuItem).DisposeWith(disposable),
            CreateCommandMenuItem(nameof(ToggleTouchPadCommand)).DisposeWith(disposable).LinkAs(ref touchPadMenuItem),
            CreateCommandMenuItem(nameof(ToggleBoostCommand)).DisposeWith(disposable).LinkAs(ref boostMenuItem),
            CreateSeparator().DisposeWith(disposable),
            CreateCommandMenuItem(nameof(SettingsCommand)).DisposeWith(disposable).LinkAs(ref aboutMenuItem),
            CreateCommandMenuItem(nameof(AboutCommand)).DisposeWith(disposable).LinkAs(ref aboutMenuItem),
            CreateSeparator().DisposeWith(disposable),
            CreateCommandMenuItem(nameof(ExitCommand)).DisposeWith(disposable)
            );

        SetCornerPreference(menu.Handle, DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND);

        return menu;
    }

    private CommandMenuItem CreateCommandMenuItem(string commandName, object commandParameter = null, Action<CommandMenuItem> initializator = null)
    {
        var item = new CommandMenuItem();
        item.Margin = new Padding(0, 8, 0, 8);
        item.CommandName = commandName;
        item.CommandParameter = commandParameter;

        initializator?.Invoke(item);

        return item;
    }

    private ToolStripSeparator CreateSeparator(ToolStripItem dependsOn = null)
    {
        var separator = new ToolStripSeparator();

        if (dependsOn != null)
        {
            dependsOn.VisibleChanged += (sender, e) => separator.Visible = dependsOn.Visible;
        }

        return separator;
    }

    private void UpdateNotifyIcon(bool isTabletMode, bool isDarkMode, int dpi)
    {
        notifyIcon.Icon = null;

        if (isDarkMode)
        {
            notifyIcon.Icon = isTabletMode
                ? imageSource.GetIcon(Images.TabletWhite, dpi)
                : imageSource.GetIcon(Images.NotebookWhite, dpi);
        }
        else
        {
            notifyIcon.Icon = isTabletMode
                ? imageSource.GetIcon(Images.Tablet, dpi)
                : imageSource.GetIcon(Images.Notebook, dpi);
        }
    }

    private void UpdateDpi(int dpi)
    {
        notifyIcon.ContextMenuStrip.Font?.Dispose();
        notifyIcon.ContextMenuStrip.Font = new Font("Segoe UI Variable Display", 10, GraphicsUnit.Point);//  13 * (dpi / 96f), GraphicsUnit.Pixel);
    }

    private class Menu : ContextMenuStrip
    {
        public Menu()
        {
            Renderer = new MenuRenderer();

            AllowTransparency = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                (Renderer as IDisposable)?.Dispose();
                Renderer = null;
            }

            base.Dispose(disposing);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            EnableAcrylic(this, Color.FromArgb(210, 44, 44, 44));
            base.OnHandleCreated(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Transparent);
        }
    }

    private class MenuRenderer : ToolStripRenderer, IDisposable
    {
        private Brush selectedBrush;
        private Pen separatorPen;
        private CompositeDisposable disposable;

        public MenuRenderer()
        {
            disposable = new CompositeDisposable();

            separatorPen = new Pen(Color.FromArgb(255, 96, 96, 96), 2).DisposeWith(disposable);
            selectedBrush = new SolidBrush(Color.FromArgb(255, 25, 110, 191)).DisposeWith(disposable);
        }

        public void Dispose()
        {
            disposable?.Dispose();
            disposable = null;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            var textHeight = e.TextFont.GetHeight(e.Graphics);
            var point = new PointF(
                e.TextRectangle.X,
                e.TextRectangle.Y + (e.TextRectangle.Height - textHeight) / 2);

            e.Graphics.DrawString(
                e.Text,
                e.TextFont,
                Brushes.White,
                point
                );
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            var y = e.Item.ContentRectangle.Y + e.Item.ContentRectangle.Height / 2;

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

                var diameter = 8; 
                var path = GetRoundedRectPath(x, y, width, height, diameter);

                e.Graphics.FillPath(selectedBrush, path);
            }
        }

        private static GraphicsPath GetRoundedRectPath(int x, int y, int width, int height, int diameter)
        {
            var arc = new Rectangle(x, y, diameter, diameter);
            var path = new GraphicsPath();

            path.AddArc(arc, 180, 90);

            arc.X = x + width - diameter;
            path.AddArc(arc, 270, 90);

            arc.Y = y + height - diameter;
            path.AddArc(arc, 0, 90);

            arc.X = x;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();

            return path;
        }
    }
}
