using System.Runtime.Serialization;
using Newtonsoft.Json;
using System;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [Serializable] // Should never be serialized! But just in case, it works.
    public struct TemporaryVoxelHandle : IEquatable<TemporaryVoxelHandle>
    {
        public static TemporaryVoxelHandle InvalidHandle = new TemporaryVoxelHandle(new GlobalVoxelCoordinate(0, 0, 0));

        #region Cache

        [JsonIgnore]
        private VoxelChunk _cache_Chunk;

        [JsonIgnore]
        private int _cache_Index;

        private void UpdateCache(ChunkData Chunks)
        {
            _cache_Index = VoxelConstants.DataIndexOf(Coordinate.GetLocalVoxelCoordinate());
            var chunkCoord = Coordinate.GetGlobalChunkCoordinate();
            _cache_Chunk = Chunks.CheckBounds(chunkCoord) ? Chunks.GetChunk(chunkCoord) : null;
        }

        [JsonIgnore]
        public VoxelChunk Chunk { get { return _cache_Chunk; } }

        #endregion

        public readonly GlobalVoxelCoordinate Coordinate;
        
        [JsonIgnore]
        public Vector3 WorldPosition { get { return Coordinate.ToVector3(); } }

        [JsonIgnore]
        public bool IsValid { get { return _cache_Chunk != null; } }

        public TemporaryVoxelHandle(ChunkData Chunks, GlobalVoxelCoordinate Coordinate)
        {
            this.Coordinate = Coordinate;
            this._cache_Chunk = null;
            this._cache_Index = 0;
            UpdateCache(Chunks);
        }

        [JsonConstructor]
        private TemporaryVoxelHandle(GlobalVoxelCoordinate Coordinate)
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

        public TemporaryVoxelHandle(VoxelChunk Chunk, LocalVoxelCoordinate Coordinate)
        {
            this.Coordinate = Chunk.ID + Coordinate;
            this._cache_Chunk = Chunk;
            this._cache_Index = VoxelConstants.DataIndexOf(Coordinate);
        }

        #region Equality
        public static bool operator ==(TemporaryVoxelHandle A, TemporaryVoxelHandle B)
        {
            return A.Coordinate == B.Coordinate;
        }

        public static bool operator !=(TemporaryVoxelHandle A, TemporaryVoxelHandle B)
        {
            return A.Coordinate != B.Coordinate;
        }

        public override int GetHashCode()
        {
            return Coordinate.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TemporaryVoxelHandle)) return false;
            return this == (TemporaryVoxelHandle)obj;
        }

        public bool Equals(TemporaryVoxelHandle other)
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

            // Change actual data
            _cache_Chunk.Data.Types[_cache_Index] = (byte)NewType.ID;
            _cache_Chunk.Data.Health[_cache_Index] = (byte)NewType.StartingHealth;

            // Did we go from empty to filled or vice versa? Update filled counter.
            if (previous == 0 && NewType.ID != 0)
                _cache_Chunk.Data.VoxelsPresentInSlice[Coordinate.Y] += 1;
            else if (previous != 0 && NewType.ID == 0)
                _cache_Chunk.Data.VoxelsPresentInSlice[Coordinate.Y] -= 1;

            if (Coordinate.Y < VoxelConstants.ChunkSizeY - 1)
                InvalidateVoxel(_cache_Chunk, Coordinate, Coordinate.Y + 1);
            InvalidateVoxel(_cache_Chunk, Coordinate, Coordinate.Y);

            // Propogate sunlight (or lack thereof) downwards.
            if (Coordinate.Y > 0)
            {
                var localCoordinate = Coordinate.GetLocalVoxelCoordinate();
                var Y = localCoordinate.Y - 1;
                var sunColor = (NewType.ID == 0 || NewType.IsTransparent) ? this.SunColor : 0;
                var below = TemporaryVoxelHandle.InvalidHandle;

                while (Y >= 0)
                {
                    below = new TemporaryVoxelHandle(Chunk, new LocalVoxelCoordinate(localCoordinate.X, Y,
                        localCoordinate.Z));
                    below.SunColor = sunColor;
                    InvalidateVoxel(Chunk, new GlobalVoxelCoordinate(Coordinate.X, Y, Coordinate.Z), Y);
                    if (!below.IsEmpty && !below.Type.IsTransparent)
                        break;
                    Y -= 1;
                }
            }
        }

        private static void InvalidateVoxel(
            VoxelChunk Chunk,
            GlobalVoxelCoordinate Coordinate,
            int Y)
        {
            Chunk.InvalidateSlice(Coordinate.Y);

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

        [JsonIgnore]
        public bool IsVisible
        {
            get { return Coordinate.Y < _cache_Chunk.Manager.ChunkData.MaxViewingLevel; }
        }

        public BoundingBox GetBoundingBox()
        {
            var pos = Coordinate.ToVector3();
            return new BoundingBox(pos, pos + Vector3.One);
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

        /// <summary>
        /// Set IsExplored without invoking the invalidation mechanism.
        /// </summary>
        /// <param name="Value"></param>
        public void RawSetIsExplored(bool Value)
        {
            _cache_Chunk.Data.IsExplored[_cache_Index] = Value;
        }

        [JsonIgnore]
        public WaterCell WaterCell
        {
            get { return _cache_Chunk.Data.Water[_cache_Index]; }
            set {

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

        public override string ToString()
        {
            return "voxel at " + Coordinate.ToString();
        }
    }
}
