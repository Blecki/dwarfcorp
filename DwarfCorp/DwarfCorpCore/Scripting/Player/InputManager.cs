using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    ///     Wraps XNA's keyboard/mouse input to allow for a "callback" style instead of a "polling" style.
    ///     This can be used to have specific functions called when a key is pressed.
    /// </summary>
    public class InputManager
    {
        public delegate void OnKeyPressed(Keys key);

        public delegate void OnKeyReleased(Keys key);

        public delegate void OnMouseClicked(MouseButton button);

        public delegate void OnMousePressed(MouseButton button);

        public delegate void OnMouseReleased(MouseButton button);

        public delegate void OnMouseScrolled(int amount);

        public enum MouseButton
        {
            Left,
            Right,
            Middle
        }

        public static Keys[] KeyValues = (Keys[]) Enum.GetValues(typeof (Keys));
        public static MouseButton[] MouseValues = (MouseButton[]) Enum.GetValues(typeof (MouseButton));

        public InputManager()
        {
            KeyStates = new Dictionary<Keys, KeyState>();
            ButtonStates = new Dictionary<MouseButton, ButtonState>();
            KeysInit(Keyboard.GetState());
            MouseInit(Mouse.GetState());
            InitializeNumKeys();
            MousePressedCallback += dummymousepressed;
            MouseReleasedCallback += dummymousereleased;
            MouseClickedCallback += dummymouseclicked;
            MouseScrolledCallback += dummymousescroll;
            KeyPressedCallback += dummykeypressed;
            KeyReleasedCallback += dummykeypressed;
        }

        public static Dictionary<Keys, KeyState> KeyStates { get; set; }
        public static Dictionary<MouseButton, ButtonState> ButtonStates { get; set; }
        public static Dictionary<Keys, int> NumberKeys { get; set; }
        public static event OnMousePressed MousePressedCallback;
        public static event OnMouseReleased MouseReleasedCallback;
        public static event OnMouseClicked MouseClickedCallback;
        public static event OnMouseScrolled MouseScrolledCallback;

        private static void OnMouseScrolledCallback(int amount)
        {
            OnMouseScrolled handler = MouseScrolledCallback;
            if (handler != null)
            {
                handler(amount);
            }
        }


        public static event OnKeyPressed KeyPressedCallback;
        public static event OnKeyReleased KeyReleasedCallback;

        public void InitializeNumKeys()
        {
            NumberKeys = new Dictionary<Keys, int>();
            NumberKeys[Keys.D0] = 0;
            NumberKeys[Keys.D1] = 1;
            NumberKeys[Keys.D2] = 2;
            NumberKeys[Keys.D3] = 3;
            NumberKeys[Keys.D4] = 4;
            NumberKeys[Keys.D5] = 5;
            NumberKeys[Keys.D6] = 6;
            NumberKeys[Keys.D7] = 7;
            NumberKeys[Keys.D8] = 8;
            NumberKeys[Keys.D9] = 9;
            NumberKeys[Keys.NumPad0] = 0;
            NumberKeys[Keys.NumPad1] = 1;
            NumberKeys[Keys.NumPad2] = 2;
            NumberKeys[Keys.NumPad3] = 3;
            NumberKeys[Keys.NumPad4] = 4;
            NumberKeys[Keys.NumPad5] = 5;
            NumberKeys[Keys.NumPad6] = 6;
            NumberKeys[Keys.NumPad7] = 7;
            NumberKeys[Keys.NumPad8] = 8;
            NumberKeys[Keys.NumPad9] = 9;
        }


        private void dummymousescroll(int amount)
        {
        }

        private void dummykeypressed(Keys k)
        {
        }

        private void dummykeyreleased(Keys k)
        {
        }

        private void dummymousepressed(MouseButton m)
        {
        }

        private void dummymousereleased(MouseButton m)
        {
        }

        private void dummymouseclicked(MouseButton m)
        {
        }

        /// <summary>
        ///     Tries to convert keyboard input to characters and prevents repeatedly returning the
        ///     same character if a key was pressed last frame, but not yet unpressed this frame.
        /// </summary>
        /// <param Name="keyboard">The current KeyboardState</param>
        /// <param Name="oldKeyboard">The KeyboardState of the previous frame</param>
        /// <param Name="key">
        ///     When this method returns, contains the correct character if conversion succeeded.
        ///     Else contains the null, (000), character.
        /// </param>
        /// <returns>True if conversion was successful</returns>
        public static bool TryConvertKeyboardInput(Keys pressed, bool shift, out char key)
        {
            switch (pressed)
            {
                    //Alphabet keys
                case Keys.A:
                    if (shift)
                    {
                        key = 'A';
                    }
                    else
                    {
                        key = 'a';
                    }
                    return true;
                case Keys.B:
                    if (shift)
                    {
                        key = 'B';
                    }
                    else
                    {
                        key = 'b';
                    }
                    return true;
                case Keys.C:
                    if (shift)
                    {
                        key = 'C';
                    }
                    else
                    {
                        key = 'c';
                    }
                    return true;
                case Keys.D:
                    if (shift)
                    {
                        key = 'D';
                    }
                    else
                    {
                        key = 'd';
                    }
                    return true;
                case Keys.E:
                    if (shift)
                    {
                        key = 'E';
                    }
                    else
                    {
                        key = 'e';
                    }
                    return true;
                case Keys.F:
                    if (shift)
                    {
                        key = 'F';
                    }
                    else
                    {
                        key = 'f';
                    }
                    return true;
                case Keys.G:
                    if (shift)
                    {
                        key = 'G';
                    }
                    else
                    {
                        key = 'g';
                    }
                    return true;
                case Keys.H:
                    if (shift)
                    {
                        key = 'H';
                    }
                    else
                    {
                        key = 'h';
                    }
                    return true;
                case Keys.I:
                    if (shift)
                    {
                        key = 'I';
                    }
                    else
                    {
                        key = 'i';
                    }
                    return true;
                case Keys.J:
                    if (shift)
                    {
                        key = 'J';
                    }
                    else
                    {
                        key = 'j';
                    }
                    return true;
                case Keys.K:
                    if (shift)
                    {
                        key = 'K';
                    }
                    else
                    {
                        key = 'k';
                    }
                    return true;
                case Keys.L:
                    if (shift)
                    {
                        key = 'L';
                    }
                    else
                    {
                        key = 'l';
                    }
                    return true;
                case Keys.M:
                    if (shift)
                    {
                        key = 'M';
                    }
                    else
                    {
                        key = 'm';
                    }
                    return true;
                case Keys.N:
                    if (shift)
                    {
                        key = 'N';
                    }
                    else
                    {
                        key = 'n';
                    }
                    return true;
                case Keys.O:
                    if (shift)
                    {
                        key = 'O';
                    }
                    else
                    {
                        key = 'o';
                    }
                    return true;
                case Keys.P:
                    if (shift)
                    {
                        key = 'P';
                    }
                    else
                    {
                        key = 'p';
                    }
                    return true;
                case Keys.Q:
                    if (shift)
                    {
                        key = 'Q';
                    }
                    else
                    {
                        key = 'q';
                    }
                    return true;
                case Keys.R:
                    if (shift)
                    {
                        key = 'R';
                    }
                    else
                    {
                        key = 'r';
                    }
                    return true;
                case Keys.S:
                    if (shift)
                    {
                        key = 'S';
                    }
                    else
                    {
                        key = 's';
                    }
                    return true;
                case Keys.T:
                    if (shift)
                    {
                        key = 'T';
                    }
                    else
                    {
                        key = 't';
                    }
                    return true;
                case Keys.U:
                    if (shift)
                    {
                        key = 'U';
                    }
                    else
                    {
                        key = 'u';
                    }
                    return true;
                case Keys.V:
                    if (shift)
                    {
                        key = 'V';
                    }
                    else
                    {
                        key = 'v';
                    }
                    return true;
                case Keys.W:
                    if (shift)
                    {
                        key = 'W';
                    }
                    else
                    {
                        key = 'w';
                    }
                    return true;
                case Keys.X:
                    if (shift)
                    {
                        key = 'X';
                    }
                    else
                    {
                        key = 'x';
                    }
                    return true;
                case Keys.Y:
                    if (shift)
                    {
                        key = 'Y';
                    }
                    else
                    {
                        key = 'y';
                    }
                    return true;
                case Keys.Z:
                    if (shift)
                    {
                        key = 'Z';
                    }
                    else
                    {
                        key = 'z';
                    }
                    return true;

                    //Decimal keys
                case Keys.D0:
                    if (shift)
                    {
                        key = ')';
                    }
                    else
                    {
                        key = '0';
                    }
                    return true;
                case Keys.D1:
                    if (shift)
                    {
                        key = '!';
                    }
                    else
                    {
                        key = '1';
                    }
                    return true;
                case Keys.D2:
                    if (shift)
                    {
                        key = '@';
                    }
                    else
                    {
                        key = '2';
                    }
                    return true;
                case Keys.D3:
                    if (shift)
                    {
                        key = '#';
                    }
                    else
                    {
                        key = '3';
                    }
                    return true;
                case Keys.D4:
                    if (shift)
                    {
                        key = '$';
                    }
                    else
                    {
                        key = '4';
                    }
                    return true;
                case Keys.D5:
                    if (shift)
                    {
                        key = '%';
                    }
                    else
                    {
                        key = '5';
                    }
                    return true;
                case Keys.D6:
                    if (shift)
                    {
                        key = '^';
                    }
                    else
                    {
                        key = '6';
                    }
                    return true;
                case Keys.D7:
                    if (shift)
                    {
                        key = '&';
                    }
                    else
                    {
                        key = '7';
                    }
                    return true;
                case Keys.D8:
                    if (shift)
                    {
                        key = '*';
                    }
                    else
                    {
                        key = '8';
                    }
                    return true;
                case Keys.D9:
                    if (shift)
                    {
                        key = '(';
                    }
                    else
                    {
                        key = '9';
                    }
                    return true;

                    //Decimal numpad keys
                case Keys.NumPad0:
                    key = '0';
                    return true;
                case Keys.NumPad1:
                    key = '1';
                    return true;
                case Keys.NumPad2:
                    key = '2';
                    return true;
                case Keys.NumPad3:
                    key = '3';
                    return true;
                case Keys.NumPad4:
                    key = '4';
                    return true;
                case Keys.NumPad5:
                    key = '5';
                    return true;
                case Keys.NumPad6:
                    key = '6';
                    return true;
                case Keys.NumPad7:
                    key = '7';
                    return true;
                case Keys.NumPad8:
                    key = '8';
                    return true;
                case Keys.NumPad9:
                    key = '9';
                    return true;

                    //Special keys
                case Keys.OemTilde:
                    if (shift)
                    {
                        key = '~';
                    }
                    else
                    {
                        key = '`';
                    }
                    return true;
                case Keys.OemSemicolon:
                    if (shift)
                    {
                        key = ':';
                    }
                    else
                    {
                        key = ';';
                    }
                    return true;
                case Keys.OemQuotes:
                    if (shift)
                    {
                        key = '"';
                    }
                    else
                    {
                        key = '\'';
                    }
                    return true;
                case Keys.OemQuestion:
                    if (shift)
                    {
                        key = '?';
                    }
                    else
                    {
                        key = '/';
                    }
                    return true;
                case Keys.OemPlus:
                    if (shift)
                    {
                        key = '+';
                    }
                    else
                    {
                        key = '=';
                    }
                    return true;
                case Keys.OemPipe:
                    if (shift)
                    {
                        key = '|';
                    }
                    else
                    {
                        key = '\\';
                    }
                    return true;
                case Keys.OemPeriod:
                    if (shift)
                    {
                        key = '>';
                    }
                    else
                    {
                        key = '.';
                    }
                    return true;
                case Keys.OemOpenBrackets:
                    if (shift)
                    {
                        key = '{';
                    }
                    else
                    {
                        key = '[';
                    }
                    return true;
                case Keys.OemCloseBrackets:
                    if (shift)
                    {
                        key = '}';
                    }
                    else
                    {
                        key = ']';
                    }
                    return true;
                case Keys.OemMinus:
                    if (shift)
                    {
                        key = '_';
                    }
                    else
                    {
                        key = '-';
                    }
                    return true;
                case Keys.OemComma:
                    if (shift)
                    {
                        key = '<';
                    }
                    else
                    {
                        key = ',';
                    }
                    return true;
                case Keys.Space:
                    key = ' ';
                    return true;
            }


            key = (char) 0;
            return false;
        }

        public static void KeysInit(KeyboardState keyState)
        {
            foreach (Keys key in KeyValues)
            {
                if (keyState.IsKeyDown(key))
                {
                    KeyStates[key] = KeyState.Down;
                }
                else
                {
                    KeyStates[key] = KeyState.Up;
                }
            }
        }

        public static void MouseInit(MouseState mouseState)
        {
            foreach (MouseButton button in MouseValues)
            {
                var state = ButtonState.Pressed;
                switch (button)
                {
                    case MouseButton.Right:
                        state = mouseState.LeftButton;
                        break;
                    case MouseButton.Left:
                        state = mouseState.RightButton;
                        break;
                    case MouseButton.Middle:
                        state = mouseState.MiddleButton;
                        break;
                }
                ButtonStates[button] = state;
            }
        }

        public static void MouseUpate(MouseState mouseState)
        {
            foreach (MouseButton button in MouseValues)
            {
                var state = ButtonState.Pressed;
                switch (button)
                {
                    case MouseButton.Right:
                        state = mouseState.RightButton;
                        break;
                    case MouseButton.Left:
                        state = mouseState.LeftButton;
                        break;
                    case MouseButton.Middle:
                        state = mouseState.MiddleButton;
                        break;
                }

                if (state == ButtonState.Pressed)
                {
                    if (ButtonStates[button] == ButtonState.Released)
                    {
                        MousePressedCallback(button);
                    }

                    ButtonStates[button] = ButtonState.Pressed;
                }
                else
                {
                    if (ButtonStates[button] == ButtonState.Pressed)
                    {
                        MouseReleasedCallback(button);
                        MouseClickedCallback(button);
                    }
                    ButtonStates[button] = ButtonState.Released;
                }
            }
        }

        public static void KeysUpdate(KeyboardState keyState)
        {
            foreach (Keys key in KeyValues)
            {
                if (keyState.IsKeyDown(key))
                {
                    if (KeyStates[key] == KeyState.Up)
                    {
                        KeyPressedCallback(key);
                        KeyStates[key] = KeyState.Down;
                    }
                }
                else if (keyState.IsKeyUp(key))
                {
                    if (KeyStates[key] == KeyState.Down)
                    {
                        KeyReleasedCallback(key);
                        KeyStates[key] = KeyState.Up;
                    }
                }
            }
        }

        public void Update()
        {
            KeyboardState keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();
            KeysUpdate(keyState);
            MouseUpate(mouseState);
        }

        public static bool IsNumKey(Keys key)
        {
            return NumberKeys.ContainsKey(key);
        }

        public static int GetNum(Keys key)
        {
            if (IsNumKey(key))
            {
                return NumberKeys[key];
            }
            return -1;
        }
    }
}