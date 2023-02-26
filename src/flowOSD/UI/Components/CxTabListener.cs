using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace flowOSD.UI.Components
{
    internal sealed class CxTabListener
    {
        private bool showKeyboardFocus = false;

        public bool ShowKeyboardFocus
        {
            get => showKeyboardFocus;
            set
            {
                if (showKeyboardFocus == value)
                {
                    return;
                }

                showKeyboardFocus = value;
                ShowKeyboardFocusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler ShowKeyboardFocusChanged;
    }
}
