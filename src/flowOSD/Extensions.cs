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
    using System.Reactive.Disposables;
    using System.Windows.Forms;

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

        public static void TraceException(Exception ex, string message)
        {
            Trace.WriteLine($"{DateTime.Now} EXCEPTION: {message}");
            Trace.Indent();
            Trace.WriteLine(ex);
            Trace.Unindent();
            Trace.Flush();
        }

        public static T Add<T>(this T control, params Control[] controls) where T : Control
        {
            control.Controls.AddRange(controls);

            return control;
        }

        public static T Add<T>(this T toolStrip, params ToolStripItem[] items) where T : ToolStrip
        {
            toolStrip.Items.AddRange(items);

            return toolStrip;
        }

        public static T Add<T, TC>(this T panel, int column, int row, Action<TC> initializator)
            where T : TableLayoutPanel
            where TC : Control, new()
        {
            var obj = Activator.CreateInstance<TC>();
            initializator(obj);
            return Add<T>(panel, column, row, obj);
        }

        public static T Add<T, TC>(this T panel, int column, int row, int columnSpan, int rowSpan, Action<TC> initializator)
            where T : TableLayoutPanel
            where TC : Control, new()
        {
            var obj = Activator.CreateInstance<TC>();
            initializator(obj);
            return Add<T>(panel, column, row, columnSpan, rowSpan, obj);
        }

        public static T Add<T>(this T panel, int column, int row, Control control) where T : TableLayoutPanel
        {
            panel.Controls.Add(control, column, row);

            return panel;
        }

        public static T Add<T>(this T panel, int column, int row, int columnSpan, int rowSpan, Control control) where T : TableLayoutPanel
        {
            panel.Controls.Add(control, column, row);
            panel.SetColumnSpan(control, columnSpan);
            panel.SetRowSpan(control, rowSpan);

            return panel;
        }
    }
}