using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DwarfCorp;

namespace ABTest
{
    public class IntegerBoundingBox
    {
        public Point3 Min;
        public Point3 Max;

        public int Width { get { return Max.X - Min.X; } }
        public int Height { get { return Max.Y - Min.Y; } }
        public int Depth { get { return Max.Z - Min.Z; } }

        public IntegerBoundingBox(Point3 Min, Point3 Max)
        {
            this.Min = Min;
            this.Max = Max;
        }

        public bool Contains(Point3 P)
        {
            return P.X >= Min.X && P.X < Max.X &&
                   P.Y >= Min.Y && P.Y < Max.Y &&
                   P.Z >= Min.Z && P.Z < Max.Z;
        }

        public bool Intersects(IntegerBoundingBox Other)
        {
            if (Min.X > Other.Max.X || Max.X <= Other.Min.X) return false;
            if (Min.Y > Other.Max.Y || Max.Y <= Other.Min.Y) return false;
            if (Min.Z > Other.Max.Z || Max.Z <= Other.Min.Z) return false;
            return true;
        }
    }
}
