using System.Runtime.Serialization;
using Newtonsoft.Json;
using System;

namespace DwarfCorp
{
    [Serializable] // Should never be serialized! But just in case, it works.
    public struct TemporaryVoxelHandle : IEquatable<TemporaryVoxelHandle>
    {
        #region Cache
        [JsonIgnore]
        private VoxelChunk _cache_Chunk;

        [JsonIgnore]
        private int _cache_Index;

        private void UpdateCache(ChunkData Chunks)
        {
            _cache_Index = VoxelConstants.DataIndexOf(Coordinate.GetLocalVoxelCoordinate());
            Chunks.ChunkMap.TryGetValue(Coordinate.GetGlobalChunkCoordinate(),
                out _cache_Chunk);
        }
        #endregion

        public readonly GlobalVoxelCoordinate Coordinate;

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

        [JsonIgnore]
        public VoxelType Type
        {
            get
            {
                return VoxelType.TypeList[_cache_Chunk.Data.Types[_cache_Index]];
            }
            set
            {
                _cache_Chunk.Data.Types[_cache_Index] = (byte)value.ID;
                _cache_Chunk.Data.Health[_cache_Index] = (byte)value.StartingHealth;
            }
        }

        #endregion
    }
}
