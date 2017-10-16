using System.Runtime.Serialization;
using Newtonsoft.Json;
using System;
using Microsoft.Xna.Framework;
using System.ComponentModel;
using System.Globalization;

namespace DwarfCorp
{
    [Serializable] // Should never be serialized! But just in case, it works.
    public struct VoxelHandle : IEquatable<VoxelHandle>
    {
        public static VoxelHandle InvalidHandle = new VoxelHandle(new GlobalVoxelCoordinate(0, 0, 0));

        #region Cache

        [JsonIgnore]
        private VoxelChunk _cache_Chunk;

        [JsonIgnore]
        private int _cache_Index;

        private void UpdateCache(ChunkData Chunks)
        {
            // Were inlining the coordinate conversions because we can gain a few cycles from not
            //  calculating the Y coordinates and from function call overhead.

            if (Coordinate.Y < 0 || Coordinate.Y >= VoxelConstants.ChunkSizeY)
                goto Invalid;

            var sX = (Coordinate.X & 0x80000000) >> 31;
            var sZ = (Coordinate.Z & 0x80000000) >> 31;

            // If the world was always at 0,0 this could be simplified further.
            // Inline GlobalVoxelCoordinate.GetGlobalChunkCoordinate
            // Inline ChunkData.CheckBounds
            var chunkX = (Coordinate.X >> VoxelConstants.XDivShift) - sX;
            if (chunkX < Chunks.ChunkMapMinX || chunkX >= Chunks.ChunkMapMinX + Chunks.ChunkMapWidth)
                goto Invalid;

            var chunkZ = (Coordinate.Z >> VoxelConstants.ZDivShift) - sZ;
            if (chunkZ < Chunks.ChunkMapMinZ || chunkZ >= Chunks.ChunkMapMinZ + Chunks.ChunkMapHeight)
                goto Invalid;

            // Inline ChunkData.GetChunk
            _cache_Chunk = Chunks.ChunkMap[(chunkZ - Chunks.ChunkMapMinZ) * Chunks.ChunkMapWidth
                + (chunkX - Chunks.ChunkMapMinX)];

            // Inline GlobalVoxelCoordinate.GetLocalVoxelCoordinate
            var localX = (sX << VoxelConstants.XDivShift) + (Coordinate.X & VoxelConstants.XModMask) - sX;
            var localZ = (sZ << VoxelConstants.ZDivShift) + (Coordinate.Z & VoxelConstants.ZModMask) - sZ;

            // Inline VoxelConstants.DataIndexOf
            _cache_Index = (Int32)((Coordinate.Y * VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ) +
                (localZ * VoxelConstants.ChunkSizeX) + localX);

            return;

            Invalid:
            _cache_Chunk = null;
            _cache_Index = 0;
        }

        [JsonIgnore]
        public VoxelChunk Chunk { get { return _cache_Chunk; } }

        #endregion

        public readonly GlobalVoxelCoordinate Coordinate;

        [JsonIgnore]
        public Vector3 WorldPosition { get { return Coordinate.ToVector3(); } }

        [JsonIgnore]
        public bool IsValid { get { return _cache_Chunk != null; } }

        public BoundingBox GetBoundingBox()
        {
            var pos = Coordinate.ToVector3();
            return new BoundingBox(pos, pos + Vector3.One);
        }

        public VoxelHandle(ChunkData Chunks, GlobalVoxelCoordinate Coordinate)
        {
            this.Coordinate = Coordinate;
            this._cache_Chunk = null;
            this._cache_Index = 0;
            UpdateCache(Chunks);
        }

        [JsonConstructor]
        internal VoxelHandle(GlobalVoxelCoordinate Coordinate)
        {
            this.Coordinate = Coordinate;
            this._cache_Chunk = null;
            this._cache_Index = 0;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            UpdateCache(((WorldManager)context.Context).ChunkManager.ChunkData);
        }

        public VoxelHandle(VoxelChunk Chunk, LocalVoxelCoordinate Coordinate)
        {
            this.Coordinate = Chunk.ID + Coordinate;
            this._cache_Chunk = Chunk;
            this._cache_Index = VoxelConstants.DataIndexOf(Coordinate);
        }

        #region Equality
        public static bool operator ==(VoxelHandle A, VoxelHandle B)
        {
            return A.Coordinate == B.Coordinate;
        }

        public static bool operator !=(VoxelHandle A, VoxelHandle B)
        {
            return A.Coordinate != B.Coordinate;
        }

        public override int GetHashCode()
        {
            return Coordinate.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VoxelHandle)) return false;
            return this == (VoxelHandle)obj;
        }

        public bool Equals(VoxelHandle other)
        {
            return this == other;
        }
        #endregion

        #region Voxel Properties

        [JsonIgnore]
        public RampType RampType
        {
            get { return _cache_Chunk.Data.RampTypes[_cache_Index]; }
            set { _cache_Chunk.Data.RampTypes[_cache_Index] = value; }
        }

        [JsonIgnore]
        public bool IsEmpty
        {
            get { return TypeID == 0; }
        }


        [JsonIgnore]
        public byte TypeID
        {
            get
            {
                return _cache_Chunk.Data.Types[_cache_Index];
            }
            set
            {
                _cache_Chunk.Data.Types[_cache_Index] = value;
                OnTypeSet(VoxelType.TypeList[value]);
            }
        }

        // Todo: Eliminate members that aren't straight pass throughs to the underlying data.
        [JsonIgnore]
        public VoxelType Type
        {
            get
            {
                return VoxelType.TypeList[_cache_Chunk.Data.Types[_cache_Index]];
            }
            set
            {
                OnTypeSet(value);
            }
        }

        [JsonIgnore]
        public bool IsVisible
        {
            get { return Coordinate.Y < _cache_Chunk.Manager.ChunkData.MaxViewingLevel; }
        }

        [JsonIgnore]
        public int SunColor
        {
            get { return _cache_Chunk.Data.SunColors[_cache_Index]; }
            set { _cache_Chunk.Data.SunColors[_cache_Index] = (byte)value; }
        }

        [JsonIgnore]
        public bool IsExplored
        {
            get { return !GameSettings.Default.FogofWar || _cache_Chunk.Data.IsExplored[_cache_Index]; }
            set
            {
                _cache_Chunk.Data.IsExplored[_cache_Index] = value;
                InvalidateVoxel(_cache_Chunk, Coordinate, Coordinate.Y);
            }
        }

        [JsonIgnore]
        public WaterCell WaterCell
        {
            get { return _cache_Chunk.Data.Water[_cache_Index]; }
            set
            {
                var existingLiquid = _cache_Chunk.Data.Water[_cache_Index];
                if (existingLiquid.Type != LiquidType.None && value.Type == LiquidType.None)
                    _cache_Chunk.Data.LiquidPresent[Coordinate.Y] -= 1;
                if (existingLiquid.Type == LiquidType.None && value.Type != LiquidType.None)
                    _cache_Chunk.Data.LiquidPresent[Coordinate.Y] += 1;

                _cache_Chunk.Data.Water[_cache_Index] = value;
            }
        }

        [JsonIgnore]
        public float Health
        {
            get { return (float)_cache_Chunk.Data.Health[_cache_Index]; }
            set
            {
                // Todo: Bad spot for this. Ideally is checked by whatever is trying to damage the voxel.
                if (Type.IsInvincible) return;
                _cache_Chunk.Data.Health[_cache_Index] = (byte)(Math.Max(Math.Min(value, 255.0f), 0.0f));
            }
        }

        #endregion

        #region Chunk Invalidation

        /// <summary>
        /// Set IsExplored without invoking the invalidation mechanism.
        /// Should only be used by ChunkGenerator as it can break geometry building.
        /// </summary>
        /// <param name="Value"></param>
        public void RawSetIsExplored(bool Value)
        {
            _cache_Chunk.Data.IsExplored[_cache_Index] = Value;
        }

        /// <summary>
        /// Set the type of a voxel without triggering all the bookkeeping mechanisms. 
        /// Should only be used by ChunkGenerator as it can break geometry building.
        /// </summary>
        /// <param name="NewType"></param>
        public void RawSetType(VoxelType NewType)
        {
            var previous = _cache_Chunk.Data.Types[_cache_Index];

            // Change actual data
            _cache_Chunk.Data.Types[_cache_Index] = (byte)NewType.ID;
            _cache_Chunk.Data.Health[_cache_Index] = (byte)NewType.StartingHealth;

            // Did we go from empty to filled or vice versa? Update filled counter.
            if (previous == 0 && NewType.ID != 0)
                _cache_Chunk.Data.VoxelsPresentInSlice[Coordinate.Y] += 1;
            else if (previous != 0 && NewType.ID == 0)
                _cache_Chunk.Data.VoxelsPresentInSlice[Coordinate.Y] -= 1;
        }

        private void OnTypeSet(VoxelType NewType)
        {
            // Changing a voxel is actually a relatively rare event, so we can afford to do a bit of 
            // bookkeeping here.

            var previous = _cache_Chunk.Data.Types[_cache_Index];
            var blockDestroyed = false;

            // Change actual data
            _cache_Chunk.Data.Types[_cache_Index] = (byte)NewType.ID;
            _cache_Chunk.Data.Health[_cache_Index] = (byte)NewType.StartingHealth;

            // Did we go from empty to filled or vice versa? Update filled counter.
            if (previous == 0 && NewType.ID != 0)
                _cache_Chunk.Data.VoxelsPresentInSlice[Coordinate.Y] += 1;
            else if (previous != 0 && NewType.ID == 0)
            {
                blockDestroyed = true;
                _cache_Chunk.Data.VoxelsPresentInSlice[Coordinate.Y] -= 1;
            }

            if (Coordinate.Y < VoxelConstants.ChunkSizeY - 1)
                InvalidateVoxel(_cache_Chunk, Coordinate, Coordinate.Y + 1);
            InvalidateVoxel(_cache_Chunk, Coordinate, Coordinate.Y);

            // Propogate sunlight (or lack thereof) downwards.
            if (Coordinate.Y > 0)
            {
                var localCoordinate = Coordinate.GetLocalVoxelCoordinate();
                var Y = localCoordinate.Y - 1;
                var sunColor = (NewType.ID == 0 || NewType.IsTransparent) ? this.SunColor : 0;
                var below = VoxelHandle.InvalidHandle;

                while (Y >= 0)
                {
                    below = new VoxelHandle(Chunk, new LocalVoxelCoordinate(localCoordinate.X, Y,
                        localCoordinate.Z));
                    below.SunColor = sunColor;
                    InvalidateVoxel(Chunk, new GlobalVoxelCoordinate(Coordinate.X, Y, Coordinate.Z), Y);
                    if (!below.IsEmpty && !below.Type.IsTransparent)
                        break;
                    Y -= 1;
                }
            }

            if (blockDestroyed)
            {
                // Invoke old voxel listener.
                _cache_Chunk.NotifyDestroyed(Coordinate.GetLocalVoxelCoordinate());
                VoxelHelpers.Reveal(_cache_Chunk.Manager.ChunkData, this);
            }

            // Invoke new voxel listener.
            _cache_Chunk.Manager.NotifyChangedVoxel(this, previous, (int)NewType.ID);
        }

        private static void InvalidateVoxel(
            VoxelChunk Chunk,
            GlobalVoxelCoordinate Coordinate,
            int Y)
        {
            Chunk.InvalidateSlice(Y);

            var localCoordinate = Coordinate.GetLocalVoxelCoordinate();

            // Invalidate slice cache for neighbor chunks.
            if (localCoordinate.X == 0)
            {
                InvalidateNeighborSlice(Chunk.Manager.ChunkData, Chunk.ID, new Point3(-1, 0, 0), Y);
                if (localCoordinate.Z == 0)
                    InvalidateNeighborSlice(Chunk.Manager.ChunkData, Chunk.ID, new Point3(-1, 0, -1), Y);
                if (localCoordinate.Z == VoxelConstants.ChunkSizeZ - 1)
                    InvalidateNeighborSlice(Chunk.Manager.ChunkData, Chunk.ID, new Point3(-1, 0, 1), Y);
            }

            if (localCoordinate.X == VoxelConstants.ChunkSizeX - 1)
            {
                InvalidateNeighborSlice(Chunk.Manager.ChunkData, Chunk.ID, new Point3(1, 0, 0), Y);
                if (localCoordinate.Z == 0)
                    InvalidateNeighborSlice(Chunk.Manager.ChunkData, Chunk.ID, new Point3(1, 0, -1), Y);
                if (localCoordinate.Z == VoxelConstants.ChunkSizeZ - 1)
                    InvalidateNeighborSlice(Chunk.Manager.ChunkData, Chunk.ID, new Point3(1, 0, 1), Y);
            }

            if (localCoordinate.Z == 0)
                InvalidateNeighborSlice(Chunk.Manager.ChunkData, Chunk.ID, new Point3(0, 0, -1), Y);

            if (localCoordinate.Z == VoxelConstants.ChunkSizeZ - 1)
                InvalidateNeighborSlice(Chunk.Manager.ChunkData, Chunk.ID, new Point3(0, 0, 1), Y);
        }

        private static void InvalidateNeighborSlice(ChunkData Chunks, GlobalChunkCoordinate ChunkCoordinate,
            Point3 NeighborOffset, int Y)
        {
            var neighborCoordinate = new GlobalChunkCoordinate(
                ChunkCoordinate.X + NeighborOffset.X,
                ChunkCoordinate.Y + NeighborOffset.Y,
                ChunkCoordinate.Z + NeighborOffset.Z);

            if (Chunks.CheckBounds(neighborCoordinate))
            {
                var chunk = Chunks.GetChunk(neighborCoordinate);
                chunk.InvalidateSlice(Y);
            }
        }

        #endregion

        public override string ToString()
        {
            return "voxel at " + Coordinate.ToString();
        }
    }
}
