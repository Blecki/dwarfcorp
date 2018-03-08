using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Rail
{
    public enum CompassOrientation
    {
        North = 0,
        Northeast = 1,
        East = 2,
        Southeast = 3,
        South = 4,
        Southwest = 5,
        West = 6,
        Northwest = 7
    }

    public static class CompassOrientationHelper
    {
        public static GlobalVoxelOffset GetOffset(CompassOrientation Orientation)
        {
            switch (Orientation)
            {
                case CompassOrientation.North:
                    return new GlobalVoxelOffset(0, 0, 1);
                case CompassOrientation.Northeast:
                    return new GlobalVoxelOffset(1, 0, 1);
                case CompassOrientation.East:
                    return new GlobalVoxelOffset(1, 0, 0);
                case CompassOrientation.Southeast:
                    return new GlobalVoxelOffset(1, 0, -1);
                case CompassOrientation.South:
                    return new GlobalVoxelOffset(0, 0, -1);
                case CompassOrientation.Southwest:
                    return new GlobalVoxelOffset(-1, 0, -1);
                case CompassOrientation.West:
                    return new GlobalVoxelOffset(-1, 0, 0);
                case CompassOrientation.Northwest:
                    return new GlobalVoxelOffset(-1, 0, 1);
                default:
                    return new GlobalVoxelOffset(0,0,0);
            }
        }

        public static CompassOrientation GetVoxelDelta(GlobalVoxelCoordinate From, GlobalVoxelCoordinate To)
        {
            if (To.X > From.X) // East
            {
                if (To.Z > From.Z)
                    return CompassOrientation.Northeast;
                else if (To.Z < From.Z)
                    return CompassOrientation.Southeast;
                else
                    return CompassOrientation.East;
            }
            else if (To.X < From.X) // West
            {
                if (To.Z > From.Z)
                    return CompassOrientation.Northwest;
                else if (To.Z < From.Z)
                    return CompassOrientation.Southwest;
                else
                    return CompassOrientation.West;
            }
            else
            {
                if (To.Z > From.Z)
                    return CompassOrientation.North;
                else if (To.Z < From.Z)
                    return CompassOrientation.South;
            }

            return CompassOrientation.North;
        }

        public static CompassOrientation Rotate(CompassOrientation Orientation, int Distance)
        {
            return (CompassOrientation)(((int)Orientation + Distance) % 8);
        }

        public static CompassOrientation Opposite(CompassOrientation Orientation)
        {
            return Rotate(Orientation, 4);
        }

    }

    public struct CompassConnection : IEquatable<CompassConnection>
    {
        public CompassOrientation A;
        public CompassOrientation B;

        public CompassConnection(CompassOrientation A, CompassOrientation B)
        {
            this.A = A;
            this.B = B;
        }

        public static bool operator ==(CompassConnection A, CompassConnection B)
        {
            if (A.A == B.A && A.B == B.B) return true;
            if (A.A == B.B && A.B == B.A) return true;
            return false;
        }

        public static bool operator !=(CompassConnection A, CompassConnection B)
        {
            return !(A == B);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CompassConnection)) return false;
            return this == (CompassConnection)obj;
        }

        public bool Equals(CompassConnection other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return ((int)A << 8) + (int)B;
        }

        public CompassConnection RotateToPiece(PieceOrientation PieceOrientation)
        {
            return new CompassConnection
            {
                A = (CompassOrientation)(((int)A + ((int)PieceOrientation * 2)) % 8),
                B = (CompassOrientation)(((int)B + ((int)PieceOrientation * 2)) % 8)
            };
        }
    }

    public class CompassConnectionConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = serializer.Deserialize<JValue>(reader);
            var tokens = jObject.Value.ToString().Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            return new CompassConnection
            {
                A = (CompassOrientation)Enum.Parse(typeof(CompassOrientation), tokens[0]),
                B = (CompassOrientation)Enum.Parse(typeof(CompassOrientation), tokens[1])
            };
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(CompassConnection);
        }
    }
}
