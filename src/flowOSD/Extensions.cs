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
namespace flowOSD;

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Reactive.Disposables;
using flowOSD.Api;
using static Native;

static class Extensions
{
    public static T Create<T>(Action<T> initializator) where T : new()
    {
        var obj = Activator.CreateInstance<T>();
        initializator(obj);

        return obj;
    }

    public static T Create<T>() where T : new()
    {
        var obj = Activator.CreateInstance<T>();

        return obj;
    }

    public static T DisposeWith<T>(this T obj, CompositeDisposable compositeDisposable) where T : IDisposable
    {
        compositeDisposable.Add(obj);

        return obj;
    }

    public static T LinkAs<T>(this T obj, ref T variable)
    {
        variable = obj;

        return obj;
    }

    public static T To<T>(this T obj, ref IList<T> list)
    {
        list.Add(obj);

        return obj;
    }

    public static void TraceException(Exception ex, string message)
    {
        Trace.WriteLine($"{DateTime.Now} EXCEPTION: {message}");
        Trace.Indent();
        Trace.WriteLine(ex);
        Trace.Unindent();
        Trace.Flush();
    }

    public static T Add<T>(this Panel control, Action<T> initializator) where T : Control
    {
        var obj = Activator.CreateInstance<T>();
        initializator(obj);

        control.Controls.Add(obj);

        return obj;
    }

    public static T Add<T>(this T control, params Control[] controls) where T : Control
    {
        control.Controls.AddRange(controls);

        return control;
    }

    public static TableLayoutPanel Add<T>(this TableLayoutPanel panel, int column, int row, Action<T> initializator)
        where T : Control, new()
    {
        var obj = Activator.CreateInstance<T>();
        initializator(obj);
        return Add(panel, column, row, obj);
    }

    public static TableLayoutPanel Add<T>(this TableLayoutPanel panel, int column, int row, int columnSpan, int rowSpan, Action<T> initializator)
        where T : Control, new()
    {
        var obj = Activator.CreateInstance<T>();
        initializator(obj);
        return Add(panel, column, row, columnSpan, rowSpan, obj);
    }

    public static TableLayoutPanel Add(this TableLayoutPanel panel, int column, int row, Control control)
    {
        panel.Controls.Add(control, column, row);

        return panel;
    }

    public static TableLayoutPanel Add(this TableLayoutPanel panel, int column, int row, int columnSpan, int rowSpan, Control control)
    {
        panel.Controls.Add(control, column, row);
        panel.SetColumnSpan(control, columnSpan);
        panel.SetRowSpan(control, rowSpan);

        return panel;
    }

    public static IDisposable SubscribeToUpdateDpi(this IMessageQueue messageQueue, Control control)
    {
        return messageQueue
            .Subscribe(WM_DPICHANGED, (x, w, l) => SendMessage(control.Handle, WM_DPICHANGED_BEFOREPARENT, w, l));
    }

    public static int DpiScale(this Control control, int value)
    {
        return (int)Math.Round(value * (GetDpiForWindow(control.Handle) / 94f));
    }

    public static Size DpiScale(this Control control, Size size)
    {
        return new Size(DpiScale(control, size.Width), DpiScale(control, size.Height));
    }

    public static Padding DpiScale(this Control control, Padding padding)
    {
        return new Padding(
            DpiScale(control, padding.Left),
            DpiScale(control, padding.Top),
            DpiScale(control, padding.Right),
            DpiScale(control, padding.Bottom));
    }

    #region Drawing

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int r)
    {
        DrawRoundedRectangle(g, pen, rect.X, rect.Y, rect.Width, rect.Height, r);
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, int x, int y, int width, int height, int r)
    {
        using var path = GetRoundedRectPath(x, y, width, height, r);

        g.DrawPath(pen, path);
    }

    public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int r)
    {
        FillRoundedRectangle(g, brush, rect.X, rect.Y, rect.Width, rect.Height, r);
    }

    public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int r)
    {
        using var path = GetRoundedRectPath(x, y, width, height, r);

        g.FillPath(brush, path);
    }

    public static GraphicsPath GetRoundedRectPath(int x, int y, int width, int height, int r)
    {
        var arc = new Rectangle(x, y, r * 2, r * 2);
        var path = new GraphicsPath();

        path.AddArc(arc, 180, 90);

        arc.X = x + width - r * 2;
        path.AddArc(arc, 270, 90);

        arc.Y = y + height - r * 2;
        path.AddArc(arc, 0, 90);

        arc.X = x;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();

        return path;
    }

    public static bool IsBright(this Color color)
    {
        return color.GetBrightness() > 0.6;
    }

    public static Color SetAlpha(this Color color, byte alpha)
    {
        return Color.FromArgb(alpha, color);
    }

    public static Color Luminance(this Color color, float factor)
    {
        ColorRGBToHLS(
            ColorTranslator.ToWin32(color),
            out int hue,
            out int luminance,
            out int saturation);

        return ColorTranslator.FromWin32(
            ColorHLSToRGB(
                hue,
                (int)Math.Round(luminance * (1 + factor)),
                saturation));
    }

    #endregion
}