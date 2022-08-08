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

using System.Reactive.Linq;
using System.Reactive.Subjects;
using flowOSD.Api;
using static Native;

sealed class NativeUI : NativeWindow, IDisposable
{
    private BehaviorSubject<int> dpiSubject;
    private IMessageQueue messageQueue;

    public NativeUI(IntPtr handle, IMessageQueue messageQueue)
    {
        this.messageQueue = messageQueue;

        dpiSubject = new BehaviorSubject<int>(GetDpiForWindow(handle));

        Dpi = dpiSubject.AsObservable();

        AssignHandle(handle);
    }

    ~NativeUI()
    {
        Dispose(false);
    }

    void IDisposable.Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        ReleaseHandle();
    }

    public IObservable<int> Dpi { get; }

    protected override void WndProc(ref Message message)
    {
        const int WM_DPICHANGED = 0x02E0;

        messageQueue.Push(ref message);

        if (message.Msg == WM_DPICHANGED)
        {
            dpiSubject.OnNext((int)HiWord(message.WParam));
        }

        base.WndProc(ref message);
    }
}