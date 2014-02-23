using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class DynamicLight
    {
        public byte Range { get; set; }
        public byte Intensity { get; set; }
        public VoxelRef Voxel { get; set; }
        public ChunkManager Chunks { get; set; }

        public DynamicLight(byte range, byte intensity, VoxelRef voxel, ChunkManager chunks)
        {
            Range = range;
            Intensity = intensity;
            Voxel = voxel;
            Chunks = chunks;
        }

        public void Destroy()
        {
            VoxelChunk chunk = Chunks.ChunkMap[Voxel.ChunkID];
            chunk.DynamicLights.Remove(this);
            Chunks.DynamicLights.Remove(this);
            foreach (VoxelChunk neighbor in chunk.Neighbors.Values)
            {
                neighbor.ShouldRebuild = true;
                neighbor.ShouldRecalculateLighting = true;
                neighbor.ResetDynamicLight(0);
            }
        }

    }
}
