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

        [JsonIgnore]
        private int _cache_Local_Y;

        private void UpdateCache(ChunkData Chunks)
        {
            // Were inlining the coordinate conversions because we can gain a few cycles from function call overhead.

            var sX = (Coordinate.X & 0x80000000) >> 31;
            var sY = (Coordinate.Y & 0x80000000) >> 31;
            var sZ = (Coordinate.Z & 0x80000000) >> 31;

            // If the world was always at 0,0 this could be simplified further.
            // Inline GlobalVoxelCoordinate.GetGlobalChunkCoordinate
            // Inline ChunkData.CheckBounds
            var chunkX = (Coordinate.X >> VoxelConstants.XDivShift) - sX;
            if (chunkX < Chunks.MapOrigin.X || chunkX >= Chunks.MapOrigin.X + Chunks.MapDimensions.X)
                goto Invalid;

            var chunkY = (Coordinate.Y >> VoxelConstants.YDivShift) - sY;
            if (chunkY < Chunks.MapOrigin.Y || chunkY >= Chunks.MapOrigin.Y + Chunks.MapDimensions.Y)
                goto Invalid;

            var chunkZ = (Coordinate.Z >> VoxelConstants.ZDivShift) - sZ;
            if (chunkZ < Chunks.MapOrigin.Z || chunkZ >= Chunks.MapOrigin.Z + Chunks.MapDimensions.Z)
                goto Invalid;

            // Inline ChunkData.GetChunk
            _cache_Chunk = Chunks.ChunkMap[
                (chunkY - Chunks.MapOrigin.Y) * Chunks.MapDimensions.X * Chunks.MapDimensions.Z
                + (chunkZ - Chunks.MapOrigin.Z) * Chunks.MapDimensions.X
                + (chunkX - Chunks.MapOrigin.X)];

            // Inline GlobalVoxelCoordinate.GetLocalVoxelCoordinate
            var localX = (sX << VoxelConstants.XDivShift) + (Coordinate.X & VoxelConstants.XModMask) - sX;
            var localY = (sY << VoxelConstants.YDivShift) + (Coordinate.Y & VoxelConstants.YModMask) - sY;
            var localZ = (sZ << VoxelConstants.ZDivShift) + (Coordinate.Z & VoxelConstants.ZModMask) - sZ;

            // Inline VoxelConstants.DataIndexOf
            _cache_Index = (Int32)((localY * VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ) +
                (localZ * VoxelConstants.ChunkSizeX) + localX);

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

        public readonly GlobalVoxelCoordinate Coordinate;

        [JsonIgnore]
        public Vector3 WorldPosition { get { return Coordinate.ToVector3(); } }

        [JsonIgnore]
        public Vector3 Center { get { return WorldPosition + new Vector3(0.5f, 0.5f, 0.5f); } }

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
            this._cache_Local_Y = 0;
            UpdateCache(Chunks);
        }

        [JsonConstructor]
        internal VoxelHandle(GlobalVoxelCoordinate Coordinate)
        {
            this.Coordinate = Coordinate;
            this._cache_Chunk = null;
            this._cache_Index = 0;
            this._cache_Local_Y = 0;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            UpdateCache(((WorldManager)context.Context).ChunkManager.ChunkData);
        }

        private VoxelHandle(VoxelChunk Chunk, LocalVoxelCoordinate Coordinate)
        {
            this.Coordinate = Chunk.ID + Coordinate;
            this._cache_Chunk = Chunk;
            this._cache_Index = VoxelConstants.DataIndexOf(Coordinate);
            this._cache_Local_Y = Coordinate.Y;
        }

        public static VoxelHandle UnsafeCreateLocalHandle(VoxelChunk Chunk, LocalVoxelCoordinate Coordinate)
        {
            return new VoxelHandle(Chunk, Coordinate);
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
            get { return (RampType)(_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.RampTypeMask); }
            set {
                if (value != (RampType)(_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.RampTypeMask))
                    _cache_Chunk.Manager.NotifyChangedVoxel(new VoxelChangeEvent
                    {
                        Type = VoxelChangeEventType.RampsChanged,
                        Voxel = this,
                        OldRamps = (RampType)(_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.RampTypeMask),
                        NewRamps = value
                    });
                _cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] = (byte)((_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.InverseRampTypeMask) | ((byte)value & VoxelConstants.RampTypeMask));
            }
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
                //_cache_Chunk.Data.Types[_cache_Index] = value;
                OnTypeSet(VoxelLibrary.TypeList[value]);
            }
        }

        // Todo: Eliminate members that aren't straight pass throughs to the underlying data.
        [JsonIgnore]
        public VoxelType Type
        {
            get
            {
                return VoxelLibrary.TypeList?[_cache_Chunk.Data.Types[_cache_Index]];
            }
            set
            {
                OnTypeSet(value);
            }
        }

        [JsonIgnore]
        public bool IsVisible
        {
            get { return Coordinate.Y < _cache_Chunk.Manager.World.Master.MaxViewingLevel; }
        }

        [JsonIgnore]
        public bool Sunlight
        {
            get { return (_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.SunlightMask) != 0; }
            set
            {
                _cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] = (byte)((_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.InverseSunlightMask) |
                  (value ? VoxelConstants.SunlightMask : 0x0));
            }
        }

        [JsonIgnore]
        public bool IsExplored
        {
            get { return !GameSettings.Default.FogofWar || (_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.ExploredMask) != 0; }
            set
            {
                // This only ever changes from false to true, so we can take advantage of that fact.
                if (value && (_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.ExploredMask) == 0)
                {
                    _cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] = (byte)((_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.InverseExploredMask) | VoxelConstants.ExploredMask);
                    InvalidateVoxel(this);

                    _cache_Chunk.Manager.NotifyChangedVoxel(new VoxelChangeEvent
                    {
                        Type = VoxelChangeEventType.Explored,
                        Voxel = this
                    });
                }
            }
        }

        [JsonIgnore]
        public bool IsPlayerBuilt
        {
            get { return (_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.PlayerBuiltVoxelMask) != 0; }
            set
            {
                _cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] = (byte)((_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.InversePlayerBuiltVoxelMask) |
                  (value ? VoxelConstants.PlayerBuiltVoxelMask : 0x0));
            }
        }

        [JsonIgnore]
        public byte GrassType
        {
            get { return (byte)(_cache_Chunk.Data.Grass[_cache_Index] >> VoxelConstants.GrassTypeShift); }
            set
            {
                _cache_Chunk.Data.Grass[_cache_Index] = (byte)((_cache_Chunk.Data.Grass[_cache_Index] & VoxelConstants.GrassDecayMask) | (value << VoxelConstants.GrassTypeShift));
               InvalidateVoxel(this);
            }
        }

        [JsonIgnore]
        public byte GrassDecay
        {
            get { return (byte)(_cache_Chunk.Data.Grass[_cache_Index] & VoxelConstants.GrassDecayMask); }
            set { _cache_Chunk.Data.Grass[_cache_Index] = (byte)((_cache_Chunk.Data.Grass[_cache_Index] & VoxelConstants.GrassTypeMask) | (value & VoxelConstants.GrassDecayMask)); }
        }

        [JsonIgnore]
        public LiquidType LiquidType
        {
            get { return (LiquidType)((_cache_Chunk.Data._Water[_cache_Index] & VoxelConstants.LiquidTypeMask) >> VoxelConstants.LiquidTypeShift); }
            set
            {
                var existingLiquid = (LiquidType)((_cache_Chunk.Data._Water[_cache_Index] & VoxelConstants.LiquidTypeMask) >> VoxelConstants.LiquidTypeShift);
                if (existingLiquid != LiquidType.None && value == LiquidType.None)
                    _cache_Chunk.Data.LiquidPresent[_cache_Local_Y] -= 1;
                if (existingLiquid == LiquidType.None && value != LiquidType.None)
                    _cache_Chunk.Data.LiquidPresent[_cache_Local_Y] += 1;

                _cache_Chunk.Data._Water[_cache_Index] = (byte)((_cache_Chunk.Data._Water[_cache_Index] & VoxelConstants.InverseLiquidTypeMask) 
                    | ((byte)value << VoxelConstants.LiquidTypeShift));
            }
        }

        [JsonIgnore]
        public byte LiquidLevel
        {
            get { return (byte)(_cache_Chunk.Data._Water[_cache_Index] & VoxelConstants.LiquidLevelMask); }
            set {
                _cache_Chunk.Data._Water[_cache_Index] = (byte)((_cache_Chunk.Data._Water[_cache_Index] & VoxelConstants.InverseLiquidLevelMask) 
                    | (value & VoxelConstants.LiquidLevelMask));
            }
        }

        /// <summary>
        /// Use when setting both type and level at once. Slightly faster.
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="Level"></param>
        public void QuickSetLiquid(LiquidType Type, byte Level)
        {
            var existingLiquid = (LiquidType)((_cache_Chunk.Data._Water[_cache_Index] & VoxelConstants.LiquidTypeMask) >> VoxelConstants.LiquidTypeShift);
            if (existingLiquid != LiquidType.None && Type == LiquidType.None)
                _cache_Chunk.Data.LiquidPresent[_cache_Local_Y] -= 1;
            if (existingLiquid == LiquidType.None && Type != LiquidType.None)
                _cache_Chunk.Data.LiquidPresent[_cache_Local_Y] += 1;

            _cache_Chunk.Data._Water[_cache_Index] = (byte)(((byte)Type << VoxelConstants.LiquidTypeShift) | (Level & VoxelConstants.LiquidLevelMask));
        }
        
        #endregion

        #region Chunk Invalidation

        /// <summary>
        /// Set IsExplored without invoking the invalidation mechanism.
        /// Should only be used by ChunkGenerator as it can break geometry building.
        /// </summary>
        /// <param name="Value"></param>
        public void RawSetIsExplored()
        {
            _cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] = (byte)((_cache_Chunk.Data.RampsSunlightExploredPlayerBuilt[_cache_Index] & VoxelConstants.InverseExploredMask) | VoxelConstants.ExploredMask);
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

            // Changing the voxel type clears grass.
            _cache_Chunk.Data.Grass[_cache_Index] = 0;

            // Did we go from empty to filled or vice versa? Update filled counter.
            if (previous == 0 && NewType.ID != 0)
                _cache_Chunk.Data.VoxelsPresentInSlice[_cache_Local_Y] += 1;
            else if (previous != 0 && NewType.ID == 0)
                _cache_Chunk.Data.VoxelsPresentInSlice[_cache_Local_Y] -= 1;
        }

        /// <summary>
        /// Set the decal of the voxel without triggering all the bookkeeping mechanisms.
        /// Should only be used by ChunkGenerator as it can break geometry building.
        /// </summary>
        /// <param name="Type"></param>
        public void RawSetGrass(byte Type)
        {
            _cache_Chunk.Data.Grass[_cache_Index] = (byte)((Type << VoxelConstants.GrassTypeShift) + (GrassLibrary.GetGrassType(Type).InitialDecayValue & VoxelConstants.GrassDecayMask));
        }

        private void OnTypeSet(VoxelType NewType)
        {
            // Changing a voxel is actually a relatively rare event, so we can afford to do a bit of 
            // bookkeeping here.

            var previous = _cache_Chunk.Data.Types[_cache_Index];
            var blockDestroyed = false;

            // Change actual data
            _cache_Chunk.Data.Types[_cache_Index] = (byte)NewType.ID;

            // Changing the voxel type clears grass.
            _cache_Chunk.Data.Grass[_cache_Index] = 0;

            // Did we go from empty to filled or vice versa? Update filled counter.
            if (previous == 0 && NewType.ID != 0)
                _cache_Chunk.Data.VoxelsPresentInSlice[_cache_Local_Y] += 1;
            else if (previous != 0 && NewType.ID == 0)
            {
                blockDestroyed = true;
                _cache_Chunk.Data.VoxelsPresentInSlice[_cache_Local_Y] -= 1;
            }

            var voxelAbove = VoxelHelpers.GetVoxelAbove(this);
            if (voxelAbove.IsValid)
                InvalidateVoxel(voxelAbove);
            InvalidateVoxel(this);

            // Propogate sunlight (or lack thereof) downwards.
            var sunlight = (NewType.ID == 0 || NewType.IsTransparent) ? this.Sunlight : false;
            var below = this;

            while (true)
            {
                below = VoxelHelpers.GetVoxelBelow(below);
                if (!below.IsValid)
                    break;
                below.Sunlight = sunlight;
                if (!below.IsEmpty)
                    InvalidateVoxel(below);
                if (!below.IsEmpty && !below.Type.IsTransparent)
                    break;
            }

            if (blockDestroyed)
            {
                // Reveal!
                VoxelHelpers.RadiusReveal(_cache_Chunk.Manager.ChunkData, this, 10);

                // Clear player built flag!
                IsPlayerBuilt = false;
            }

            // Invoke new voxel listener.
            _cache_Chunk.Manager.NotifyChangedVoxel(new VoxelChangeEvent
            {
                Type = VoxelChangeEventType.VoxelTypeChanged,
                Voxel = this,
                OriginalVoxelType = previous,
                NewVoxelType = NewType.ID
            });
        }

        public void Invalidate()
        {
            InvalidateVoxel(this);
        }

        private static void InvalidateVoxel(VoxelHandle Voxel)
        {
            Voxel._cache_Chunk.InvalidateSlice(Voxel._cache_Local_Y);
            var localCoordinate = Voxel.Coordinate.GetLocalVoxelCoordinate();

            // Invalidate slice cache for neighbor chunks.
            if (localCoordinate.X == 0)
            {
                InvalidateNeighborSlice(Voxel._cache_Chunk.Manager.ChunkData, Voxel._cache_Chunk.ID, new Point3(-1, 0, 0), localCoordinate.Y);
                if (localCoordinate.Z == 0)
                    InvalidateNeighborSlice(Voxel._cache_Chunk.Manager.ChunkData, Voxel._cache_Chunk.ID, new Point3(-1, 0, -1), localCoordinate.Y);
                if (localCoordinate.Z == VoxelConstants.ChunkSizeZ - 1)
                    InvalidateNeighborSlice(Voxel._cache_Chunk.Manager.ChunkData, Voxel._cache_Chunk.ID, new Point3(-1, 0, 1), localCoordinate.Y);
            }

            if (localCoordinate.X == VoxelConstants.ChunkSizeX - 1)
            {
                InvalidateNeighborSlice(Voxel._cache_Chunk.Manager.ChunkData, Voxel._cache_Chunk.ID, new Point3(1, 0, 0), localCoordinate.Y);
                if (localCoordinate.Z == 0)
                    InvalidateNeighborSlice(Voxel._cache_Chunk.Manager.ChunkData, Voxel._cache_Chunk.ID, new Point3(1, 0, -1), localCoordinate.Y);
                if (localCoordinate.Z == VoxelConstants.ChunkSizeZ - 1)
                    InvalidateNeighborSlice(Voxel._cache_Chunk.Manager.ChunkData, Voxel._cache_Chunk.ID, new Point3(1, 0, 1), localCoordinate.Y);
            }

            if (localCoordinate.Z == 0)
                InvalidateNeighborSlice(Voxel._cache_Chunk.Manager.ChunkData, Voxel._cache_Chunk.ID, new Point3(0, 0, -1), localCoordinate.Y);

            if (localCoordinate.Z == VoxelConstants.ChunkSizeZ - 1)
                InvalidateNeighborSlice(Voxel._cache_Chunk.Manager.ChunkData, Voxel._cache_Chunk.ID, new Point3(0, 0, 1), localCoordinate.Y);
        }

        private static void InvalidateNeighborSlice(ChunkData Chunks, GlobalChunkCoordinate ChunkCoordinate,
            Point3 NeighborOffset, int LocalY)
        {
            var neighborCoordinate = new GlobalChunkCoordinate(
                ChunkCoordinate.X + NeighborOffset.X,
                ChunkCoordinate.Y + NeighborOffset.Y,
                ChunkCoordinate.Z + NeighborOffset.Z);

            if (Chunks.CheckBounds(neighborCoordinate))
            {
                var chunk = Chunks.GetChunk(neighborCoordinate);
                chunk.InvalidateSlice(LocalY);
            }
        }

        #endregion

        public override string ToString()
        {
            return "voxel at " + Coordinate.ToString();
        }
    }
}
