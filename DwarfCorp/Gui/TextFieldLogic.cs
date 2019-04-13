using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Gui
{
    public class TextFieldLogic
    {
        public static String Process(String Text, int CursorPosition, int Character, out int NewCursorPosition)
        {
            CursorPosition = MathFunctions.Clamp(CursorPosition, 0, Text.Length);

            if (Character == 8) // Backspace
            {
                if (CursorPosition == 0)
                {
                    NewCursorPosition = CursorPosition;
                    return Text;
                }
                else if (CursorPosition >= Text.Length)
                {
                    NewCursorPosition = CursorPosition - 1;
                    return Text.Substring(0, Text.Length - 1);
                }
                else
                {
                    NewCursorPosition = CursorPosition - 1;
                    return Text.Substring(0, CursorPosition - 1) + Text.Substring(CursorPosition, Text.Length - CursorPosition);
                }
            }
            else if (Character == 127) // Delete
            {
                NewCursorPosition = CursorPosition;
                if (CursorPosition == 0)
                    return Text.Substring(1, Text.Length - 1);
                else if (CursorPosition >= Text.Length)
                    return Text;
                else
                    return Text.Substring(0, CursorPosition) + Text.Substring(CursorPosition + 1, Text.Length - CursorPosition - 1);
            }
            else if (Character >= 32 && Character <= 126) // Ascii printable range.
            {
                NewCursorPosition = CursorPosition + 1;
                return Text.Substring(0, CursorPosition) + new String((char)Character, 1) + Text.Substring(CursorPosition);
            }
            else
            {
                NewCursorPosition = CursorPosition;
                return Text;
            }
        }

        public static String HandleSpecialKeys(String Text, int CursorPosition, int Key, out int NewCursorPosition)
        {
            if (Key == 37 && CursorPosition > 0) // Left arrow
            {
                NewCursorPosition = CursorPosition - 1;
                return Text;
            }

            if (Key == 39 && CursorPosition < Text.Length) // Right arrow
            {
                NewCursorPosition = CursorPosition + 1;
                return Text;
            }
                
            if (Key == 46) // Delete
            {
                NewCursorPosition = CursorPosition;
                if (CursorPosition == 0 && Text.Length > 0)
                    return Text.Substring(1, Text.Length - 1);
                else if (CursorPosition >= Text.Length)
                    return Text;
                else
                    return Text.Substring(0, CursorPosition) + Text.Substring(CursorPosition + 1, Text.Length - CursorPosition - 1);
            }

            NewCursorPosition = CursorPosition;
            return Text;
        }
    }
}
