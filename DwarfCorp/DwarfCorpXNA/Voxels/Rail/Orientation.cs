using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Rail
{
    // These corrospond to decal orientation values
    public enum Orientations
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public class Orientation
    {
        public static Orientations Rotate(Orientations In, int Ammount)
        {
            return (Orientations)(((int)In + Ammount) % 4);
        }
    }
}
