using DwarfCorp.Gui;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp.Play
{
    public static class Hotkeys
    {
        public static List<Keys> KeyList = new List<Keys>
            {
                    Keys.D1,
                    Keys.D2,
                    Keys.D3,
                    Keys.D4,
                    Keys.D5,
                    Keys.D6,
                    Keys.D7,
                    Keys.D8,
                    Keys.D9,
                    Keys.D0,
                    Keys.Q,
                    Keys.W,
                    Keys.E,
                    Keys.R,
                    Keys.T,
                    Keys.Y,
                    Keys.U,
                    Keys.I,
                    Keys.O,
                    Keys.P,
                    Keys.A,
                    Keys.S,
                    Keys.D,
                    Keys.F,
                    Keys.G,
                    Keys.H,
                    Keys.J,
                    Keys.K,
                    Keys.L,
                    Keys.Z,
                    Keys.X,
                    Keys.C,
                    Keys.V,
                    Keys.B,
                    Keys.N,
                    Keys.M,
                    Keys.OemComma,
                    Keys.OemPeriod,
                    Keys.OemBackslash,
                    Keys.OemMinus,
                    Keys.OemPlus,
        };

        public static void AssignHotKeys<T>(IEnumerable<T> To, Action<T, Keys> AssignFunction)
        {
            var keyIndex = 0;
            foreach (var item in To)
            {
                while (keyIndex < KeyList.Count && ControlSettings.Mappings.Contains(KeyList[keyIndex]))
                    keyIndex += 1;
                if (keyIndex >= KeyList.Count)
                    return;
                AssignFunction(item, KeyList[keyIndex]);
                keyIndex += 1;
            }
        }
    }
}
