using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public struct Point3 : IEquatable<Point3>
    {
        public int X;
        public int Y;
        public int Z;

        public Point3(Microsoft.Xna.Framework.Vector3 vect)
        {
            X = (int)vect.X;
            Y = (int)vect.Y;
            Z = (int)vect.Z;
        }

        public Point3(int x, int y, int z) 
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override int GetHashCode()
        {
            return X ^ 2 * Y ^ 4 * Z;
        }

        public bool Equals(Point3 other)
        {
             return other.X == X && other.Y == Y && other.Z == Z;
        }

        public override bool Equals(object obj)
        {
            if (obj is Point3)
            {
                Point3 other = (Point3)obj;
                return other.X == X && other.Y == Y && other.Z == Z;
            }
            else
            {
                return false;
            }
        }

    }
}
