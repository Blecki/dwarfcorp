using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public struct GlobalVoxelCoordinate : IEquatable<GlobalVoxelCoordinate>
    {
        public Int32 X { get; }
        public Int32 Y { get; }
        public Int32 Z { get; }

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
                X / VoxelConstants.ChunkSizeX, 
                Y / VoxelConstants.ChunkSizeY, 
                Z / VoxelConstants.ChunkSizeZ);
        }

        public LocalVoxelCoordinate GetLocalVoxelCoordinate()
        {
            return new LocalVoxelCoordinate(
                X % VoxelConstants.ChunkSizeX, 
                Y % VoxelConstants.ChunkSizeY, 
                Z % VoxelConstants.ChunkSizeZ);
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
    }

    public struct GlobalVoxelOffset : IEquatable<GlobalVoxelOffset>
    {
        public Int32 X { get; }
        public Int32 Y { get; }
        public Int32 Z { get; }

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
            return (X << VoxelConstants.ChunkSizeX) + Z;
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

    public struct GlobalChunkCoordinate : IEquatable<GlobalChunkCoordinate>
    {
        public Int32 X { get; }
        public Int32 Y { get; }
        public Int32 Z { get; }

        public GlobalChunkCoordinate(Int32 X, Int32 Y, Int32 Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
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
            return (X << VoxelConstants.ChunkSizeX) + Z;
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

    public struct LocalVoxelCoordinate : IEquatable<LocalVoxelCoordinate>
    {
        public Int32 X { get; }
        public Int32 Y { get; }
        public Int32 Z { get; }

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
            return (X << VoxelConstants.ChunkSizeX) + Z;
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
