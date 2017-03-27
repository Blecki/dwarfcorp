using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace ABTest.SpacialHash
{
    public struct Point3 : IEquatable<Point3>
    {
        public int X;
        public int Y;
        public int Z;

        public Point3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3 AsVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public override int GetHashCode()
        {
            const int p1 = 4273;
            const int p2 = 6247;
            return (X * p1 + Y) * p2 + Z;
        }

        public bool Equals(Point3 other)
        {
            return other.X == X && other.Y == Y && other.Z == Z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Point3))
            {
                return false;
            }
            Point3 other = (Point3)obj;
            return other.X == X && other.Y == Y && other.Z == Z;
        }

        public override string ToString()
        {
            return String.Format("{{{0}, {1}, {2}}}", X, Y, Z);
        }
    }
}
