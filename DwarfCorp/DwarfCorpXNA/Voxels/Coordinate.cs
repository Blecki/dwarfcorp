using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public struct GlobalVoxelCoordinate : IEquatable<GlobalVoxelCoordinate>
    {
        public readonly Int32 X;
        public readonly Int32 Y;
        public readonly Int32 Z;

        [JsonConstructor]
        public GlobalVoxelCoordinate(Int32 X, Int32 Y, Int32 Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public GlobalVoxelCoordinate(GlobalChunkCoordinate C, LocalVoxelCoordinate L)
        {
            X = (C.X * VoxelConstants.ChunkSizeX) + L.X;
            Y = (C.Y * VoxelConstants.ChunkSizeY) + L.Y;
            Z = (C.Z * VoxelConstants.ChunkSizeZ) + L.Z;
        }

        public static GlobalVoxelCoordinate operator +(GlobalVoxelCoordinate A, GlobalVoxelOffset B)
        {
            return new GlobalVoxelCoordinate(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        }

        public static GlobalVoxelCoordinate operator +(GlobalVoxelOffset B, GlobalVoxelCoordinate A)
        {
            return new GlobalVoxelCoordinate(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        }

        public static GlobalVoxelOffset operator -(GlobalVoxelCoordinate A, GlobalVoxelCoordinate B)
        {
            return new GlobalVoxelOffset(A.X - B.X, A.Y - B.Y, A.Z - B.Z);
        }

        public GlobalChunkCoordinate GetGlobalChunkCoordinate()
        {
            return new GlobalChunkCoordinate(
                (Int32)((X >> VoxelConstants.XDivShift) - ((X & 0x80000000) >> 31)),
                (Int32)((Y >> VoxelConstants.YDivShift) - ((Y & 0x80000000) >> 31)),
                (Int32)((Z >> VoxelConstants.ZDivShift) - ((Z & 0x80000000) >> 31)));
        }

        public LocalVoxelCoordinate GetLocalVoxelCoordinate()
        {
            return new LocalVoxelCoordinate(
                (Int32)((((X & 0x80000000) >> 31) << VoxelConstants.XDivShift) + (X & VoxelConstants.XModMask) - ((X & 0x80000000) >> 31)),
                (Int32)((((Y & 0x80000000) >> 31) << VoxelConstants.YDivShift) + (Y & VoxelConstants.YModMask) - ((Y & 0x80000000) >> 31)),
                (Int32)((((Z & 0x80000000) >> 31) << VoxelConstants.ZDivShift) + (Z & VoxelConstants.ZModMask) - ((Z & 0x80000000) >> 31)));
        }

        public static bool operator ==(GlobalVoxelCoordinate A, GlobalVoxelCoordinate B)
        {
            return A.X == B.X && A.Y == B.Y && A.Z == B.Z;
        }

        public static bool operator !=(GlobalVoxelCoordinate A, GlobalVoxelCoordinate B)
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
            if (!(obj is GlobalVoxelCoordinate)) return false;
            return this == (GlobalVoxelCoordinate)obj;
        }

        public bool Equals(GlobalVoxelCoordinate other)
        {
            return this == other;
        }

        internal static GlobalVoxelCoordinate FromVector3(Vector3 V)
        {
            return new GlobalVoxelCoordinate(
                (int)Math.Floor(V.X), (int)Math.Floor(V.Y), (int)Math.Floor(V.Z));
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", X, Y, Z);
        }

        public static GlobalVoxelCoordinate Parse(String In)
        {
            var parts = In.Split(' ');
            if (parts.Length != 3) throw new InvalidOperationException();
            return new GlobalVoxelCoordinate(Int32.Parse(parts[0]), Int32.Parse(parts[1]), Int32.Parse(parts[2]));
        }

        public static bool TryParse(String In, out GlobalVoxelCoordinate V)
        {
            V = new GlobalVoxelCoordinate(0, 0, 0);

            var parts = In.Split(' ');
            if (parts.Length != 3)
                return false;

            Int32 x = 0, y = 0, z = 0;
            if (!Int32.TryParse(parts[0], out x)) return false;
            if (!Int32.TryParse(parts[1], out y)) return false;
            if (!Int32.TryParse(parts[2], out z)) return false;

            V = new GlobalVoxelCoordinate(x, y, z);
            return true;
        }
    }

    [Serializable]
    public struct GlobalVoxelOffset : IEquatable<GlobalVoxelOffset>
    {
        public readonly Int32 X;
        public readonly Int32 Y;
        public readonly Int32 Z;

        [JsonConstructor]
        public GlobalVoxelOffset(Int32 X, Int32 Y, Int32 Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public static GlobalVoxelOffset operator -(GlobalVoxelOffset A)
        {
            return new GlobalVoxelOffset(-A.X, -A.Y, -A.Z);
        }

        public static bool operator ==(GlobalVoxelOffset A, GlobalVoxelOffset B)
        {
            return A.X == B.X && A.Y == B.Y && A.Z == B.Z;
        }

        public static bool operator !=(GlobalVoxelOffset A, GlobalVoxelOffset B)
        {
            return A.X != B.X || A.Y != B.Y || A.Z != B.Z;
        }

        public override int GetHashCode()
        {
            return (Y << 16) + (X << 8) + Z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GlobalVoxelOffset)) return false;
            return this == (GlobalVoxelOffset)obj;
        }

        public bool Equals(GlobalVoxelOffset other)
        {
            return this == other;
        }

        internal static GlobalVoxelOffset FromVector3(Vector3 V)
        {
            return new GlobalVoxelOffset(
                (int)Math.Floor(V.X), (int)Math.Floor(V.Y), (int)Math.Floor(V.Z));
        }
    }

    [Serializable]
    public struct GlobalChunkCoordinate : IEquatable<GlobalChunkCoordinate>
    {
        public readonly Int32 X;
        public readonly Int32 Y;
        public readonly Int32 Z;

        [JsonConstructor]
        public GlobalChunkCoordinate(Int32 X, Int32 Y, Int32 Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public static GlobalVoxelCoordinate operator +(GlobalChunkCoordinate A, LocalVoxelCoordinate B)
        {
            return new GlobalVoxelCoordinate(A, B);
        }

        public static bool operator ==(GlobalChunkCoordinate A, GlobalChunkCoordinate B)
        {
            return A.X == B.X && A.Y == B.Y && A.Z == B.Z;
        }

        public static bool operator !=(GlobalChunkCoordinate A, GlobalChunkCoordinate B)
        {
            return A.X != B.X || A.Y != B.Y || A.Z != B.Z;
        }

        public override int GetHashCode()
        {
            return (X << 8) + Z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GlobalChunkCoordinate)) return false;
            return this == (GlobalChunkCoordinate)obj;
        }

        public bool Equals(GlobalChunkCoordinate other)
        {
            return this == other;
        }
    }

    [Serializable]
    public struct LocalVoxelCoordinate : IEquatable<LocalVoxelCoordinate>
    {
        public readonly Int32 X;
        public readonly Int32 Y;
        public readonly Int32 Z;

        [JsonConstructor]
        public LocalVoxelCoordinate(Int32 X, Int32 Y, Int32 Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public static bool operator ==(LocalVoxelCoordinate A, LocalVoxelCoordinate B)
        {
            return A.X == B.X && A.Y == B.Y && A.Z == B.Z;
        }

        public static bool operator !=(LocalVoxelCoordinate A, LocalVoxelCoordinate B)
        {
            return A.X != B.X || A.Y != B.Y || A.Z != B.Z;
        }

        public override int GetHashCode()
        {
            return (Y << 8) + (X << 4) + Z;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LocalVoxelCoordinate)) return false;
            return this == (LocalVoxelCoordinate)obj;
        }

        public bool Equals(LocalVoxelCoordinate other)
        {
            return this == other;
        }
    }
}
