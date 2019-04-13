using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Rail
{
    public class OrientationHelper
    {
        public static PieceOrientation Rotate(PieceOrientation In, int Ammount)
        {
            return (PieceOrientation)(((int)In + Ammount) % 4);
        }

        public static PieceOrientation Opposite(PieceOrientation In)
        {
            return Rotate(In, 2);
        }

        public static PieceOrientation Relative(PieceOrientation Base, PieceOrientation Top)
        {
            int c = 0;

            while (Base != Top)
            {
                Base = Rotate(Base, 1);
                c += 1;
            }

            return (PieceOrientation)c;
        }
    }
}
