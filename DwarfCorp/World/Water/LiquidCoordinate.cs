using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [Serializable]
    public struct GlobalLiquidCoordinate : IEquatable<GlobalLiquidCoordinate>
    {
        public readonly Int32 X;
        public readonly Int32 Y;
        public readonly Int32 Z;

        [JsonConstructor]
        public GlobalLiquidCoordinate(Int32 X, Int32 Y, Int32 Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public GlobalLiquidCoordinate(GlobalChunkCoordinate C, LocalLiquidCoordinate L)
        {
            X = (C.X * VoxelConstants.LiquidChunkSizeX) + L.X;
            Y = (C.Y * VoxelConstants.LiquidChunkSizeY) + L.Y;
            Z = (C.Z * VoxelConstants.LiquidChunkSizeZ) + L.Z;
        }

        public static GlobalLiquidCoordinate operator +(GlobalLiquidCoordinate A, GlobalLiquidOffset B)
        {
            return new GlobalLiquidCoordinate(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        }

        public static GlobalLiquidCoordinate operator +(GlobalLiquidOffset B, GlobalLiquidCoordinate A)
        {
            return new GlobalLiquidCoordinate(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        }

        public static GlobalLiquidOffset operator -(GlobalLiquidCoordinate A, GlobalLiquidCoordinate B)
        {
            return new GlobalLiquidOffset(A.X - B.X, A.Y - B.Y, A.Z - B.Z);
        }

        public GlobalChunkCoordinate GetGlobalChunkCoordinate()
        {
            return new GlobalChunkCoordinate(
                (Int32)((X >> VoxelConstants.XLiquidDivShift) - ((X & 0x80000000) >> 31)),
                (Int32)((Y >> VoxelConstants.YLiquidDivShift) - ((Y & 0x80000000) >> 31)),
                (Int32)((Z >> VoxelConstants.ZLiquidDivShift) - ((Z & 0x80000000) >> 31)));
        }

        public LocalLiquidCoordinate GetLocalLiquidCoordinate()
        {
            return new LocalLiquidCoordinate(
                (Int32)((((X & 0x80000000) >> 31) << VoxelConstants.XLiquidDivShift) + (X & VoxelConstants.XLiquidModMask) - ((X & 0x80000000) >> 31)),
                (Int32)((((Y & 0x80000000) >> 31) << VoxelConstants.YLiquidDivShift) + (Y & VoxelConstants.YLiquidModMask) - ((Y & 0x80000000) >> 31)),
                (Int32)((((Z & 0x80000000) >> 31) << VoxelConstants.ZLiquidDivShift) + (Z & VoxelConstants.ZLiquidModMask) - ((Z & 0x80000000) >> 31)));
        }

        public static bool operator ==(GlobalLiquidCoordinate A, GlobalLiquidCoordinate B)
        {
            return A.X == B.X && A.Y == B.Y && A.Z == B.Z;
        }

        public static bool operator !=(GlobalLiquidCoordinate A, GlobalLiquidCoordinate B)
        {
            return A.X != B.X || A.Y != B.Y || A.Z != B.Z;
        }

        public override int GetHashCode()
        {
            return (((Z << VoxelConstants.ZDivShift) + Y) << VoxelConstants.YDivShift) + X;
        }

        /// <summary>
        /// Get a long hash of a coordinate. As long as the world is never larger than 2^16 on any dimension,
        /// this will give unique values for every voxel.
        /// </summary>
        /// <returns></returns>
        public ulong GetLongHash()
        {
            ulong q = 0;
            q |= (((ulong)X & 0xFFFF) << 32);
            q |= (((ulong)Y & 0xFFFF) << 16);
            q |= ((ulong)Z & 0xFFFF);
            return q;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GlobalLiquidCoordinate)) return false;
            return this == (GlobalLiquidCoordinate)obj;
        }

        public bool Equals(GlobalLiquidCoordinate other)
        {
            return this == other;
        }

        internal static GlobalLiquidCoordinate FromVector3(Vector3 V)
        {
            return new GlobalLiquidCoordinate(
                (int)Math.Floor(V.X), (int)Math.Floor(V.Y), (int)Math.Floor(V.Z));
        }

        public GlobalVoxelCoordinate ToGlobalVoxelCoordinate()
        {
            return new GlobalVoxelCoordinate(X / 2, Y / 2, Z / 2); // Todo: Make these constant scaling factors.
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", X, Y, Z);
        }

        public static GlobalLiquidCoordinate Parse(String In)
        {
            var parts = In.Split(' ');
            if (parts.Length != 3) throw new InvalidOperationException();
            return new GlobalLiquidCoordinate(Int32.Parse(parts[0]), Int32.Parse(parts[1]), Int32.Parse(parts[2]));
        }

        public static bool TryParse(String In, out GlobalLiquidCoordinate V)
        {
            V = new GlobalLiquidCoordinate(0, 0, 0);

            var parts = In.Split(' ');
            if (parts.Length != 3)
                return false;

            Int32 x = 0, y = 0, z = 0;
            if (!Int32.TryParse(parts[0], out x)) return false;
            if (!Int32.TryParse(parts[1], out y)) return false;
            if (!Int32.TryParse(parts[2], out z)) return false;

            V = new GlobalLiquidCoordinate(x, y, z);
            return true;
        }
    }

    [Serializable]
    public struct GlobalLiquidOffset : IEquatable<GlobalLiquidOffset>
    {
        public readonly Int32 X;
        public readonly Int32 Y;
        public readonly Int32 Z;

        [JsonConstructor]
        public GlobalLiquidOffset(Int32 X, Int32 Y, Int32 Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public static GlobalLiquidOffset operator -(GlobalLiquidOffset A)
        {
            return new GlobalLiquidOffset(-A.X, -A.Y, -A.Z);
        }

        public static bool operator ==(GlobalLiquidOffset A, GlobalLiquidOffset B)
        {
            return A.X == B.X && A.Y == B.Y && A.Z == B.Z;
        }

        public static bool operator !=(GlobalLiquidOffset A, GlobalLiquidOffset B)
        {
            return A.X != B.X || A.Y != B.Y || A.Z != B.Z;
        }

        public override int GetHashCode()
        {
            return (Y << 16) + (X << 8) + Z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GlobalLiquidOffset)) return false;
            return this == (GlobalLiquidOffset)obj;
        }

        public bool Equals(GlobalLiquidOffset other)
        {
            return this == other;
        }

        internal static GlobalLiquidOffset FromVector3(Vector3 V)
        {
            return new GlobalLiquidOffset(
                (int)Math.Floor(V.X), (int)Math.Floor(V.Y), (int)Math.Floor(V.Z));
        }

        public Vector3 AsVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public static GlobalLiquidOffset operator +(GlobalLiquidOffset B, GlobalLiquidOffset A)
        {
            return new GlobalLiquidOffset(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        }
    }

    [Serializable]
    public struct LocalLiquidCoordinate : IEquatable<LocalLiquidCoordinate>
    {
        public readonly Int32 X;
        public readonly Int32 Y;
        public readonly Int32 Z;

        [JsonConstructor]
        public LocalLiquidCoordinate(Int32 X, Int32 Y, Int32 Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public LocalLiquidCoordinate Offset(Int32 X, Int32 Y, Int32 Z)
        {
            return new LocalLiquidCoordinate(this.X + X, this.Y + Y, this.Z + Z);
        }

        public static bool operator ==(LocalLiquidCoordinate A, LocalLiquidCoordinate B)
        {
            return A.X == B.X && A.Y == B.Y && A.Z == B.Z;
        }

        public static bool operator !=(LocalLiquidCoordinate A, LocalLiquidCoordinate B)
        {
            return A.X != B.X || A.Y != B.Y || A.Z != B.Z;
        }

        public override int GetHashCode()
        {
            return (Y << 8) + (X << 4) + Z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LocalLiquidCoordinate)) return false;
            return this == (LocalLiquidCoordinate)obj;
        }

        public bool Equals(LocalLiquidCoordinate other)
        {
            return this == other;
        }
        public override string ToString()
        {
            return String.Format("{0} {1} {2}", X, Y, Z);
        }
    }
}
