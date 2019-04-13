using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Input
{
    public class GumInputMapper
    {
        public class QueuedInput
        {
            public InputEvents Message;
            public InputEventArgs Args;
        }

        public System.Threading.Mutex QueueLock = new System.Threading.Mutex();
        public List<QueuedInput> Queued = new List<QueuedInput>();
        private MouseState OldMouseState = Mouse.GetState();
        private KeyboardState OldKeyboardState = Keyboard.GetState();
        private Dictionary<Keys, DateTime> PressedTime = new Dictionary<Keys, DateTime>();

        private bool ConsoleTogglePressed = false;

        public bool WasConsoleTogglePressed()
        {
            if (ConsoleTogglePressed)
            {
                ConsoleTogglePressed = false;
                return true;
            }
            return false;
        }

        public List<QueuedInput> GetInputQueue()
        {
            QueueLock.WaitOne();
            var r = Queued;
            Queued = new List<QueuedInput>();
            var now = DateTime.Now;
            
            // Generate mouse events.
            var newMouseState = Mouse.GetState();
            var newKeyboardState = Keyboard.GetState();

            if (newMouseState.X != OldMouseState.X || newMouseState.Y != OldMouseState.Y)
                r.Add(new QueuedInput
                    {
                        Message = InputEvents.MouseMove,
                        Args = new InputEventArgs
                        {
                            X = newMouseState.X,
                            Y = newMouseState.Y,
                            Alt = newKeyboardState.IsKeyDown(Keys.LeftAlt) || newKeyboardState.IsKeyDown(Keys.RightAlt),
                            Shift = newKeyboardState.IsKeyDown(Keys.LeftShift) || newKeyboardState.IsKeyDown(Keys.RightShift),
                            Control = newKeyboardState.IsKeyDown(Keys.LeftControl) || newKeyboardState.IsKeyDown(Keys.RightControl)
                        }
                    });

            if (newMouseState.LeftButton == ButtonState.Pressed && OldMouseState.LeftButton == ButtonState.Released)
                r.Add(new QueuedInput
                {
                    Message = InputEvents.MouseDown,
                    Args = new InputEventArgs
                    {
                        X = newMouseState.X,
                        Y = newMouseState.Y,
                        MouseButton = 0,
                        Alt = newKeyboardState.IsKeyDown(Keys.LeftAlt) || newKeyboardState.IsKeyDown(Keys.RightAlt),
                        Shift = newKeyboardState.IsKeyDown(Keys.LeftShift) || newKeyboardState.IsKeyDown(Keys.RightShift),
                        Control = newKeyboardState.IsKeyDown(Keys.LeftControl) || newKeyboardState.IsKeyDown(Keys.RightControl)
                    }
                });

            if (newMouseState.LeftButton == ButtonState.Released && OldMouseState.LeftButton == ButtonState.Pressed)
            {
                r.Add(new QueuedInput
                {
                    Message = InputEvents.MouseUp,
                    Args = new InputEventArgs
                    {
                        X = newMouseState.X,
                        Y = newMouseState.Y,
                        MouseButton = 0,
                        Alt = newKeyboardState.IsKeyDown(Keys.LeftAlt) || newKeyboardState.IsKeyDown(Keys.RightAlt),
                        Shift = newKeyboardState.IsKeyDown(Keys.LeftShift) || newKeyboardState.IsKeyDown(Keys.RightShift),
                        Control = newKeyboardState.IsKeyDown(Keys.LeftControl) || newKeyboardState.IsKeyDown(Keys.RightControl)
                    }
                });
                r.Add(new QueuedInput
                {
                    Message = InputEvents.MouseClick,
                    Args = new InputEventArgs
                    {
                        X = newMouseState.X,
                        Y = newMouseState.Y,
                        MouseButton = 0,
                        Alt = newKeyboardState.IsKeyDown(Keys.LeftAlt) || newKeyboardState.IsKeyDown(Keys.RightAlt),
                        Shift = newKeyboardState.IsKeyDown(Keys.LeftShift) || newKeyboardState.IsKeyDown(Keys.RightShift),
                        Control = newKeyboardState.IsKeyDown(Keys.LeftControl) || newKeyboardState.IsKeyDown(Keys.RightControl)
                    }
                });
            }


            if (newMouseState.RightButton == ButtonState.Pressed && OldMouseState.RightButton == ButtonState.Released)
                r.Add(new QueuedInput
                {
                    Message = InputEvents.MouseDown,
                    Args = new InputEventArgs
                    {
                        X = newMouseState.X,
                        Y = newMouseState.Y,
                        MouseButton = 1,
                        Alt = newKeyboardState.IsKeyDown(Keys.LeftAlt) || newKeyboardState.IsKeyDown(Keys.RightAlt),
                        Shift = newKeyboardState.IsKeyDown(Keys.LeftShift) || newKeyboardState.IsKeyDown(Keys.RightShift),
                        Control = newKeyboardState.IsKeyDown(Keys.LeftControl) || newKeyboardState.IsKeyDown(Keys.RightControl)
                    }
                });

            if (newMouseState.RightButton == ButtonState.Released && OldMouseState.RightButton == ButtonState.Pressed)
            {
                r.Add(new QueuedInput
                {
                    Message = InputEvents.MouseUp,
                    Args = new InputEventArgs
                    {
                        X = newMouseState.X,
                        Y = newMouseState.Y,
                        MouseButton = 1,
                        Alt = newKeyboardState.IsKeyDown(Keys.LeftAlt) || newKeyboardState.IsKeyDown(Keys.RightAlt),
                        Shift = newKeyboardState.IsKeyDown(Keys.LeftShift) || newKeyboardState.IsKeyDown(Keys.RightShift),
                        Control = newKeyboardState.IsKeyDown(Keys.LeftControl) || newKeyboardState.IsKeyDown(Keys.RightControl)
                    }
                });

                r.Add(new QueuedInput
                {
                    Message = InputEvents.MouseClick,
                    Args = new InputEventArgs
                    {
                        X = newMouseState.X,
                        Y = newMouseState.Y,
                        MouseButton = 1,
                        Alt = newKeyboardState.IsKeyDown(Keys.LeftAlt) || newKeyboardState.IsKeyDown(Keys.RightAlt),
                        Shift = newKeyboardState.IsKeyDown(Keys.LeftShift) || newKeyboardState.IsKeyDown(Keys.RightShift),
                        Control = newKeyboardState.IsKeyDown(Keys.LeftControl) || newKeyboardState.IsKeyDown(Keys.RightControl)
                    }
                });
            }

            if (newMouseState.ScrollWheelValue != OldMouseState.ScrollWheelValue)
            {
                r.Add(new QueuedInput()
                {
                    Message = InputEvents.MouseWheel,
                    Args = new InputEventArgs()
                    {
                        ScrollValue = (newMouseState.ScrollWheelValue - OldMouseState.ScrollWheelValue),
                        Alt = newKeyboardState.IsKeyDown(Keys.LeftAlt) || newKeyboardState.IsKeyDown(Keys.RightAlt),
                        Shift = newKeyboardState.IsKeyDown(Keys.LeftShift) || newKeyboardState.IsKeyDown(Keys.RightShift),
                        Control = newKeyboardState.IsKeyDown(Keys.LeftControl) || newKeyboardState.IsKeyDown(Keys.RightControl)
                    }
                });
            }

            OldMouseState = newMouseState;


            foreach (var key in newKeyboardState.GetPressedKeys())
            {
                if (!OldKeyboardState.IsKeyDown(key))
                {
                    r.Add(new QueuedInput
                    {
                        Message = InputEvents.KeyDown,
                        Args = new InputEventArgs
                        {
                            KeyValue = (int)key,
                            Alt = newKeyboardState.IsKeyDown(Keys.LeftAlt) || newKeyboardState.IsKeyDown(Keys.RightAlt),
                            Shift = newKeyboardState.IsKeyDown(Keys.LeftShift) || newKeyboardState.IsKeyDown(Keys.RightShift),
                            Control = newKeyboardState.IsKeyDown(Keys.LeftControl) || newKeyboardState.IsKeyDown(Keys.RightControl)
                        }
                    });

                    PressedTime[key] = now;
                }
                else
                {
                    if (PressedTime.ContainsKey(key) && (now - PressedTime[key]).TotalSeconds > GameSettings.Default.FNAONLY_KeyRepeatRate)
                    {
                        PressedTime[key] = now;

                        r.Add(new QueuedInput
                        {
                            Message = InputEvents.KeyDown,
                            Args = new InputEventArgs
                            {
                                KeyValue = (int)key,
                                Alt = newKeyboardState.IsKeyDown(Keys.LeftAlt) || newKeyboardState.IsKeyDown(Keys.RightAlt),
                                Shift = newKeyboardState.IsKeyDown(Keys.LeftShift) || newKeyboardState.IsKeyDown(Keys.RightShift),
                                Control = newKeyboardState.IsKeyDown(Keys.LeftControl) || newKeyboardState.IsKeyDown(Keys.RightControl)
                            }
                        });
                    }
                }
            }

            foreach (var key in OldKeyboardState.GetPressedKeys())
                if (!newKeyboardState.IsKeyDown(key))
                {
                    r.Add(new QueuedInput
                    {
                        Message = InputEvents.KeyUp,
                        Args = new InputEventArgs
                        {
                            KeyValue = (int)key,
                            Alt = newKeyboardState.IsKeyDown(Keys.LeftAlt) || newKeyboardState.IsKeyDown(Keys.RightAlt),
                            Shift = newKeyboardState.IsKeyDown(Keys.LeftShift) || newKeyboardState.IsKeyDown(Keys.RightShift),
                            Control = newKeyboardState.IsKeyDown(Keys.LeftControl) || newKeyboardState.IsKeyDown(Keys.RightControl)
                        }
                    });

                    if (key == Keys.OemTilde)
                    {
                        ConsoleTogglePressed = true;
                    }

                    if (PressedTime.ContainsKey(key))
                        PressedTime.Remove(key);
                }

            OldKeyboardState = newKeyboardState;
            
            QueueLock.ReleaseMutex();
            return r;
        }
        
        public GumInputMapper(IntPtr WindowHandle)
        {
#if !GEMMONO
            Microsoft.Xna.Framework.Input.TextInputEXT.TextInput += c =>
                {
                    QueueLock.WaitOne();
                    Queued.Add(new QueuedInput
                        {
                            Message = InputEvents.KeyPress,
                            Args = new InputEventArgs
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
#endif
        }
    }
}
