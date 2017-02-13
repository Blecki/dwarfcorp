using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

// http://romsteady.blogspot.com/2011/06/key-events-in-xna-40.html

namespace Gum.Input
{
    public class MessageFilter : IMessageFilter
    {
        [DllImport("user32.dll")]
        static extern bool TranslateMessage(ref Message lpMsg);

        const int WM_CHAR = 0x0102;
        const int WM_KEYUP = 0x101;
        const int WM_KEYDOWN = 0x0100;
        const int WM_MOUSEMOVE = 0x0200;
        const int WM_LBUTTONDOWN = 0x0201;
        const int WM_LBUTTONUP = 0x202;
        const int WM_RBUTTONDOWN = 0x204;
        const int WM_RBUTTONUP = 0x0205;

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg == WM_KEYDOWN)
                TranslateMessage(ref m);

            if (Handler != null) return Handler(m);
            return true;
        }

        private Func<System.Windows.Forms.Message, bool> Handler;

        public static void AddMessageFilter(Func<System.Windows.Forms.Message, bool> Handler)
        {
            var filter = new MessageFilter();
            filter.Handler = Handler;
            System.Windows.Forms.Application.AddMessageFilter(filter);
        }
    }
}