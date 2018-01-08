using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Rail
{
    public enum Orientation
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    public class OrientationHelper
    {
        public static Orientation Rotate(Orientation In, int Ammount)
        {
            return (Orientation)(((int)In + Ammount) % 4);
        }

        public static Orientation Relative(Orientation Base, Orientation Top)
        {
            int c = 0;

            while (Base != Top)
            {
                Base = Rotate(Base, 1);
                c += 1;
            }

            return (Orientation)c;
        }
    }
}
