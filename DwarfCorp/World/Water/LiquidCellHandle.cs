using System.Runtime.Serialization;
using Newtonsoft.Json;
using System;
using Microsoft.Xna.Framework;
using System.ComponentModel;
using System.Globalization;

namespace DwarfCorp
{
    [Serializable] // Should never be serialized! But just in case, it works.
    public struct LiquidCellHandle : IEquatable<LiquidCellHandle>
    {
        public static LiquidCellHandle InvalidHandle = new LiquidCellHandle(new GlobalLiquidCoordinate(0, 0, 0));

        #region Cache

        [JsonIgnore]
        private VoxelChunk _cache_Chunk;

        [JsonIgnore]
        private int _cache_Index;

        [JsonIgnore]
        private int _cache_Local_Y;

        private void UpdateCache(ChunkManager Chunks)
        {
            // Were inlining the coordinate conversions because we can gain a few cycles from function call overhead.

            var sX = (Coordinate.X & 0x80000000) >> 31;
            var sY = (Coordinate.Y & 0x80000000) >> 31;
            var sZ = (Coordinate.Z & 0x80000000) >> 31;

            // If the world was always at 0,0 this could be simplified further.
            // Inline GlobalVoxelCoordinate.GetGlobalChunkCoordinate
            // Inline ChunkData.CheckBounds
            var chunkX = (Coordinate.X >> VoxelConstants.XLiquidDivShift) - sX;
            if (chunkX < Chunks.MapOrigin.X || chunkX >= Chunks.MapOrigin.X + Chunks.MapDimensions.X)
                goto Invalid;

            var chunkY = (Coordinate.Y >> VoxelConstants.YLiquidDivShift) - sY;
            if (chunkY < Chunks.MapOrigin.Y || chunkY >= Chunks.MapOrigin.Y + Chunks.MapDimensions.Y)
                goto Invalid;

            var chunkZ = (Coordinate.Z >> VoxelConstants.ZLiquidDivShift) - sZ;
            if (chunkZ < Chunks.MapOrigin.Z || chunkZ >= Chunks.MapOrigin.Z + Chunks.MapDimensions.Z)
                goto Invalid;

            // Inline ChunkData.GetChunk
            _cache_Chunk = Chunks.ChunkMap[
                (chunkY - Chunks.MapOrigin.Y) * Chunks.MapDimensions.X * Chunks.MapDimensions.Z
                + (chunkZ - Chunks.MapOrigin.Z) * Chunks.MapDimensions.X
                + (chunkX - Chunks.MapOrigin.X)];

            // Inline GlobalVoxelCoordinate.GetLocalVoxelCoordinate
            var localX = (sX << VoxelConstants.XLiquidDivShift) + (Coordinate.X & VoxelConstants.XLiquidModMask) - sX;
            var localY = (sY << VoxelConstants.YLiquidDivShift) + (Coordinate.Y & VoxelConstants.YLiquidModMask) - sY;
            var localZ = (sZ << VoxelConstants.ZLiquidDivShift) + (Coordinate.Z & VoxelConstants.ZLiquidModMask) - sZ;

            // Inline VoxelConstants.DataIndexOf
            _cache_Index = (Int32)((localY * VoxelConstants.LiquidChunkSizeX * VoxelConstants.LiquidChunkSizeZ) +
                (localZ * VoxelConstants.LiquidChunkSizeX) + localX);

            _cache_Local_Y = (int)localY;

            return;

            Invalid:
            _cache_Chunk = null;
            _cache_Index = 0;
            _cache_Local_Y = 0;
        }

        [JsonIgnore]
        public VoxelChunk Chunk { get { return _cache_Chunk; } }

        #endregion

        public readonly GlobalLiquidCoordinate Coordinate;

        [JsonIgnore]
        public Vector3 WorldPosition { get { return Coordinate.ToVector3(); } } // This needs to be scaled by 1/2 once the primitive builder is fixed.

        [JsonIgnore]
        public Vector3 Center { get { return WorldPosition + new Vector3(0.5f, 0.5f, 0.5f); } }

        [JsonIgnore]
        public bool IsValid { get { return _cache_Chunk != null; } }

        public BoundingBox GetBoundingBox()
        {
            var pos = Coordinate.ToVector3();
            return new BoundingBox(pos, pos + (Vector3.One * 0.5f));
        }

        public LiquidCellHandle(ChunkManager Chunks, GlobalLiquidCoordinate Coordinate)
        {
            this.Coordinate = Coordinate;
            this._cache_Chunk = null;
            this._cache_Index = 0;
            this._cache_Local_Y = 0;
            UpdateCache(Chunks);
        }

        [JsonConstructor]
        internal LiquidCellHandle(GlobalLiquidCoordinate Coordinate)
        {
            this.Coordinate = Coordinate;
            this._cache_Chunk = null;
            this._cache_Index = 0;
            this._cache_Local_Y = 0;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            UpdateCache(((WorldManager)context.Context).ChunkManager);
        }

        private LiquidCellHandle(VoxelChunk Chunk, LocalLiquidCoordinate Coordinate)
        {
            this.Coordinate = Chunk.ID + Coordinate;
            this._cache_Chunk = Chunk;
            this._cache_Index = VoxelConstants.DataIndexOf(Coordinate);
            this._cache_Local_Y = Coordinate.Y;
        }

        public static LiquidCellHandle UnsafeCreateLocalHandle(VoxelChunk Chunk, LocalLiquidCoordinate Coordinate)
        {
            return new LiquidCellHandle(Chunk, Coordinate);
        }

        #region Equality
        public static bool operator ==(LiquidCellHandle A, LiquidCellHandle B)
        {
            return A.Coordinate == B.Coordinate;
        }

        public static bool operator !=(LiquidCellHandle A, LiquidCellHandle B)
        {
            return A.Coordinate != B.Coordinate;
        }

        public override int GetHashCode()
        {
            return Coordinate.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LiquidCellHandle)) return false;
            return this == (LiquidCellHandle)obj;
        }

        public bool Equals(LiquidCellHandle other)
        {
            return this == other;
        }
        #endregion

        #region Voxel Properties

        [JsonIgnore]
        public byte LiquidType
        {
            get { return (byte)((_cache_Chunk.Data.Liquid[_cache_Index] & VoxelConstants.LiquidTypeMask) >> VoxelConstants.LiquidTypeShift); }
            set
            {
                var existingLiquid = (byte)((_cache_Chunk.Data.Liquid[_cache_Index] & VoxelConstants.LiquidTypeMask) >> VoxelConstants.LiquidTypeShift);
                if (existingLiquid != 0 && value == 0)
                    _cache_Chunk.Data.LiquidPresent[_cache_Local_Y] -= 1;
                if (existingLiquid == 0 && value != 0)
                    _cache_Chunk.Data.LiquidPresent[_cache_Local_Y] += 1;

                _cache_Chunk.Data.Liquid[_cache_Index] = (byte)((_cache_Chunk.Data.Liquid[_cache_Index] & VoxelConstants.InverseLiquidTypeMask) 
                    | ((byte)value << VoxelConstants.LiquidTypeShift));
            }
        }

        [JsonIgnore]
        public byte LiquidLevel
        {
            get { return (byte)(_cache_Chunk.Data.Liquid[_cache_Index] & VoxelConstants.LiquidLevelMask); }
            set {
                _cache_Chunk.Data.Liquid[_cache_Index] = (byte)((_cache_Chunk.Data.Liquid[_cache_Index] & VoxelConstants.InverseLiquidLevelMask) 
                    | (value & VoxelConstants.LiquidLevelMask));
            }
        }

        /// <summary>
        /// Use when setting both type and level at once. Slightly faster.
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Level"></param>
        public void QuickSetLiquid(byte Type, byte Level)
        {
            var existingLiquid = (byte)((_cache_Chunk.Data.Liquid[_cache_Index] & VoxelConstants.LiquidTypeMask) >> VoxelConstants.LiquidTypeShift);
            if (existingLiquid != 0 && Type == 0)
                _cache_Chunk.Data.LiquidPresent[_cache_Local_Y] -= 1;
            if (existingLiquid == 0 && Type != 0)
                _cache_Chunk.Data.LiquidPresent[_cache_Local_Y] += 1;

            _cache_Chunk.Data.Liquid[_cache_Index] = (byte)(((byte)Type << VoxelConstants.LiquidTypeShift) | (Level & VoxelConstants.LiquidLevelMask));
        }
        
        #endregion

        public override string ToString()
        {
            return "liquid cell at " + Coordinate.ToString();
        }
    }
}
