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
namespace flowOSD.Services;

using flowOSD.Api;

sealed class MessageQueue : IMessageQueue, IDisposable
{
    private Dictionary<int, ICollection<Action<int, IntPtr, IntPtr>>> subscriptions;
    private Filter filter;
    private NativeWindow nativeWindow;

    public MessageQueue()
    {
        nativeWindow = new NativeUI(this);
        subscriptions = new Dictionary<int, ICollection<Action<int, IntPtr, IntPtr>>>();

        filter = new Filter(this);
        Application.AddMessageFilter(filter);
    }

    void IDisposable.Dispose()
    {
        Application.RemoveMessageFilter(filter);
    }

    public IntPtr Handle => nativeWindow.Handle;

    public IDisposable Subscribe(int messageId, Action<int, IntPtr, IntPtr> proc)
    {
        if (!subscriptions.ContainsKey(messageId))
        {
            subscriptions[messageId] = new List<Action<int, IntPtr, IntPtr>>();
        }

        subscriptions[messageId].Add(proc);

        return new Subscription(this, messageId, proc);
    }

    private void Remove(int messageId, Action<int, IntPtr, IntPtr> proc)
    {
        if (subscriptions.ContainsKey(messageId))
        {
            subscriptions[messageId].Remove(proc);
        }
    }

    private void Push(ref Message message)
    {
        if (subscriptions.ContainsKey(message.Msg))
        {
            foreach (var proc in subscriptions[message.Msg])
            {
                proc(message.Msg, message.WParam, message.LParam);
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private MessageQueue owner;
        private int messageId;
        private Action<int, IntPtr, IntPtr> proc;

        public Subscription(MessageQueue owner, int messageId, Action<int, IntPtr, IntPtr> proc)
        {
            this.owner = owner;
            this.messageId = messageId;
            this.proc = proc;
        }

        void IDisposable.Dispose()
        {
            owner.Remove(messageId, proc);
        }
    }

    private sealed class Filter : IMessageFilter
    {
        private MessageQueue queue;

        public Filter(MessageQueue queue)
        {
            this.queue = queue;
        }

        public bool PreFilterMessage(ref Message m)
        {
            queue.Push(ref m);

            return false;
        }
    }

    private sealed class NativeUI : NativeWindow, IDisposable
    {
        private MessageQueue queue;

        private Form form;

        public NativeUI(MessageQueue queue)
        {
            this.queue = queue ?? throw new ArgumentNullException(nameof(queue));

            form = new Form();
            AssignHandle(form.Handle);
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

            if (disposing)
            {
                form?.Dispose();
                form = null;
            }
        }

        protected override void WndProc(ref Message message)
        {
            queue.Push(ref message);

            base.WndProc(ref message);
        }
    }
}