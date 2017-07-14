using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [Serializable]
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
                (Int32)((((X & 0x80000000) >> 31) << VoxelConstants.XDivShift) + (X % VoxelConstants.ChunkSizeX) - ((X & 0x80000000) >> 31)),
                (Int32)((((Y & 0x80000000) >> 31) << VoxelConstants.YDivShift) + (Y % VoxelConstants.ChunkSizeY) - ((Y & 0x80000000) >> 31)),
                (Int32)((((Z & 0x80000000) >> 31) << VoxelConstants.ZDivShift) + (Z % VoxelConstants.ChunkSizeZ) - ((Z & 0x80000000) >> 31)));
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
            return (X << VoxelConstants.ChunkSizeX) + Z;
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
