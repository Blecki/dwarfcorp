using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.SteamPipes
{
    public class OrientationHelper
    {
        public static Orientation Rotate(Orientation In, int Ammount)
        {
            return (Orientation)(((int)In + Ammount) % 4);
        }

        public static Orientation Opposite(Orientation In)
        {
            return Rotate(In, 2);
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

        public static Orientation DetectOrientationFromVector(Vector3 V)
        {
            // Todo: NO NO NO North is positive Z
            // Treat unit X as north because why not.
            if (V.X > 0.5f) return Orientation.North;
            else if (V.X < -0.5f) return Orientation.South;
            else if (V.Z > 0.5f) return Orientation.East;
            else if (V.Z < -0.5f) return Orientation.West;
            return Orientation.North;
        }

        public static Orientation DetectOrientationFromRotation(Quaternion Rotation)
        {
            var unitX = new Vector3(1.0f, 0.0f, 0.0f); // Don't think unit X actually corrosponds to north in the actual game but as long as it's consistent, wgaf.
            return DetectOrientationFromVector(Vector3.Normalize(Vector3.Transform(unitX, Rotation)));
        }
    }
}
