using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DwarfCorp.Gui
{
    public enum InputEvents
    {
        KeyPress,
        KeyDown,
        KeyUp,
        MouseMove,
        MouseEnter,
        MouseLeave,
        MouseDown,
        MouseUp,
        MouseHover,
        MouseClick
    }

    public class InputEventArgs
    {
        public bool Alt;
        public bool Control;
        public bool Shift;

        /// <summary>
        /// Key pressed for keyboard events.
        /// </summary>
        public int KeyValue;

        /// <summary>
        /// X position of mouse for mouse events.
        /// </summary>
        public int X;

        /// <summary>
        /// Y position of mouse for mouse events.
        /// </summary>
        public int Y;

        public bool Handled;
    }
}
