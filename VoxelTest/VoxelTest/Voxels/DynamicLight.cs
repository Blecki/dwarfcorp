using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// Lights nearby voxels with torch lights.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class DynamicLight
    {
        public byte Range { get; set; }
        public byte Intensity { get; set; }
        public VoxelRef Voxel { get; set; }

        [JsonIgnore]
        public ChunkManager Chunks { get; set; }

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            Chunks = PlayState.ChunkManager;
        }


        public DynamicLight()
        {
            
        }

        public DynamicLight(byte range, byte intensity, VoxelRef voxel, ChunkManager chunks)
        {
            Range = range;
            Intensity = intensity;
            Voxel = voxel;
            Chunks = chunks;
        }

        public void Destroy()
        {
            VoxelChunk chunk = Chunks.ChunkData.ChunkMap[Voxel.ChunkID];
            chunk.DynamicLights.Remove(this);
            Chunks.DynamicLights.Remove(this);
            foreach(VoxelChunk neighbor in chunk.Neighbors.Values)
            {
                neighbor.ShouldRebuild = true;
                neighbor.ShouldRecalculateLighting = true;
                neighbor.ResetDynamicLight(0);
            }
        }
    }

}