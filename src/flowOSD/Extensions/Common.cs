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
using System.Collections;
using System.Diagnostics;
using System.Reactive.Disposables;

namespace flowOSD.Extensions;

static class Common
{
    public static bool IsWindows11 => Environment.OSVersion.Version.Build >= 22000;

    public static short Hi(this IntPtr value) => BitConverter.ToInt16(BitConverter.GetBytes(value), 2);

    public static short Low(this IntPtr value) => BitConverter.ToInt16(BitConverter.GetBytes(value), 0);

    public static T? FirstOrDefault<T>(this ICollection collection, Func<T, bool> condition)
    {
        foreach (var i in collection)
        {
            if (i is T item && condition(item))
            {
                return item;
            }
        }

        return default;
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

    public static void TraceWarning(string message)
    {
        Trace.WriteLine($"{DateTime.Now} WARNING: {message}");
        Trace.Flush();
    }

    public static void TraceException(Exception? ex, string message)
    {
        Trace.WriteLine($"{DateTime.Now} EXCEPTION: {message}");
        Trace.Indent();
        Trace.WriteLine(ex ?? (object)"ex is NULL");
        Trace.Unindent();
        Trace.Flush();
    }
}