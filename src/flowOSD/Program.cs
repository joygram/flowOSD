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
namespace flowOSD;

using System.Diagnostics;
using System.Reactive.Disposables;
using flowOSD.Services;
using static Extensions;

public static class Program
{
    [STAThread]
    static void Main()
    {
        var disposable = new CompositeDisposable();
        var config = new Config().DisposeWith(disposable);

#if !DEBUG
        var logFileName = Path.Combine(config.DataDirectory.FullName, "log.txt");
        var listener = new TextWriterTraceListener(logFileName);
        Trace.Listeners.Add(listener);
#endif
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            var app = new App(config).DisposeWith(disposable);

            Application.Run(app.ApplicationContext);
        }
        catch (Exception ex)
        {
            TraceException(ex, "General Failure");
            MessageBox.Show(ex.Message, "General Failure", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }
#if !DEBUG
        finally
        {
            disposable.Dispose();

            listener.Flush();
            listener.Dispose();
        }
#endif
    }
}