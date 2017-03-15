using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace Gum.Input
{
    public class GumInputMapper
    {
        public class QueuedInput
        {
            public Gum.InputEvents Message;
            public Gum.InputEventArgs Args;
        }

        public System.Threading.Mutex QueueLock = new System.Threading.Mutex();
        public List<QueuedInput> Queued = new List<QueuedInput>();
        private MouseState OldMouseState = Mouse.GetState();
        
        public List<QueuedInput> GetInputQueue()
        {
            QueueLock.WaitOne();
            var r = Queued;
            Queued = new List<QueuedInput>();
            
            // Generate mouse events.
            var newMouseState = Mouse.GetState();

            if (newMouseState.X != OldMouseState.X || newMouseState.Y != OldMouseState.Y)
                r.Add(new QueuedInput
                    {
                        Message = InputEvents.MouseMove,
                        Args = new InputEventArgs
                        {
                            X = newMouseState.X,
                            Y = newMouseState.Y
                        }
                    });

            if (newMouseState.LeftButton == ButtonState.Pressed && OldMouseState.LeftButton == ButtonState.Released)
                r.Add(new QueuedInput
                {
                    Message = Gum.InputEvents.MouseDown,
                    Args = new Gum.InputEventArgs
                    {
                        X = newMouseState.X,
                        Y = newMouseState.Y
                    }
                });

            if (newMouseState.LeftButton == ButtonState.Released && OldMouseState.LeftButton == ButtonState.Pressed)
            {
                r.Add(new QueuedInput
                {
                    Message = Gum.InputEvents.MouseUp,
                    Args = new Gum.InputEventArgs
                    {
                        X = newMouseState.X,
                        Y = newMouseState.Y
                    }
                });
                r.Add(new QueuedInput
                {
                    Message = Gum.InputEvents.MouseClick,
                    Args = new Gum.InputEventArgs
                    {
                        X = newMouseState.X,
                        Y = newMouseState.Y
                    }
                });
            }

            OldMouseState = newMouseState;
            
            QueueLock.ReleaseMutex();
            return r;
        }
        
        public GumInputMapper(IntPtr WindowHandle)
        {
            Microsoft.Xna.Framework.Input.TextInputEXT.TextInput += c =>
                {
                    QueueLock.WaitOne();
                    Queued.Add(new QueuedInput
                        {
                            Message = Gum.InputEvents.KeyPress,
                            Args = new Gum.InputEventArgs
                            {
                                KeyValue = c,
                                Alt = false,
                                Control = false,
                                Shift = false
                            }
                        });
                    QueueLock.ReleaseMutex();
                };

            Microsoft.Xna.Framework.Input.TextInputEXT.StartTextInput();
        }
    }
}
