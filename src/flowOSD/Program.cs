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
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;
    using flowOSD.Api;
    using flowOSD.Services;
    using flowOSD.UI;
    using static Extensions;

    public static class Program
    {
        [STAThread]
        static void Main()
        {
            var logFileName = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "log.txt");
            var fileListener = new TextWriterTraceListener(logFileName);

            try
            {
                Trace.Listeners.Add(fileListener);

                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

                using (var app = new App())
                {
                    Application.Run(app.ApplicationContext);
                }
            }
            catch (Exception ex)
            {
                TraceException(ex, "General Failure");
            }
            finally
            {
                fileListener.Flush();
                fileListener.Dispose();
            }
        }
    }
}