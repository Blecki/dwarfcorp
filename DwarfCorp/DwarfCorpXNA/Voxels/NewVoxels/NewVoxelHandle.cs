using System.Runtime.Serialization;
using Newtonsoft.Json;
using System;

namespace DwarfCorp
{
    // Todo: Make immutable?
    public struct NewVoxelHandle : IEquatable<NewVoxelHandle>
    {
        #region Cache
        [JsonIgnore]
        private ChunkData Chunks;

        [JsonIgnore]
        private VoxelChunk _cache_Chunk;

        [JsonIgnore]
        private int _cache_Index;

        //[JsonIgnore]
        //private LocalVoxelCoordinate _cache_LocalCoordinate;

        private void UpdateCache()
        {
            //_cache_LocalCoordinate = _backing_GlobalCoordinate.GetLocalVoxelCoordinate();
            _cache_Index = VoxelConstants.DataIndexOf(_backing_GlobalCoordinate.GetLocalVoxelCoordinate());
            Chunks.ChunkMap.TryGetValue(_backing_GlobalCoordinate.GetGlobalChunkCoordinate(),
                out _cache_Chunk);
        }
        #endregion

        [JsonProperty]
        private GlobalVoxelCoordinate _backing_GlobalCoordinate;

        [JsonIgnore]
        public GlobalVoxelCoordinate Coordinate
        {
            get { return _backing_GlobalCoordinate; }
            set
            {
                _backing_GlobalCoordinate = value;
                UpdateCache();
            }
        }

        [JsonIgnore]
        public bool IsValid { get { return _cache_Chunk != null; } }

        public NewVoxelHandle(ChunkData Chunks, GlobalVoxelCoordinate Coordinate)
        {
            this.Chunks = Chunks;
            this._cache_Chunk = null;
            //this._cache_LocalCoordinate = new LocalVoxelCoordinate(0, 0, 0);
            this._cache_Index = 0;
            this._backing_GlobalCoordinate = Coordinate;
            UpdateCache();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Chunks = ((WorldManager)context.Context).ChunkManager.ChunkData;
            UpdateCache();
        }

        #region Equality
        public static bool operator ==(NewVoxelHandle A, NewVoxelHandle B)
        {
            return A._backing_GlobalCoordinate == B._backing_GlobalCoordinate;
        }

        public static bool operator !=(NewVoxelHandle A, NewVoxelHandle B)
        {
            return A._backing_GlobalCoordinate != B._backing_GlobalCoordinate;
        }

        public override int GetHashCode()
        {
            return _backing_GlobalCoordinate.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is NewVoxelHandle)) return false;
            return this == (NewVoxelHandle)obj;
        }

        public bool Equals(NewVoxelHandle other)
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
            get { return Type.ID == 0; }
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
