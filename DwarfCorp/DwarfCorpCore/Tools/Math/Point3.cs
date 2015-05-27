using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// Just a simple data type with 3 integers (x, y, and z).
    /// </summary>
    public struct Point3 : IEquatable<Point3>
    {
        public int X;
        public int Y;
        public int Z;

        public Point3(Microsoft.Xna.Framework.Vector3 vect)
        {
            X = (int) vect.X;
            Y = (int) vect.Y;
            Z = (int) vect.Z;
        }

        public Point3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
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
            if(!(obj is Point3))
            {
                return false;
            }
            Point3 other = (Point3) obj;
            return other.X == X && other.Y == Y && other.Z == Z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }
    }

}