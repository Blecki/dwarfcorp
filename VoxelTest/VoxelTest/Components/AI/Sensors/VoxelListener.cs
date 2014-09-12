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
    /// When a voxel is destroyed, this component kills whatever it is attached to.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class VoxelListener : GameComponent
    {
        public Point3 VoxelID;

        [JsonIgnore] 
        public VoxelChunk Chunk;

        public Point3 ChunkID { get; set; }

        public VoxelListener()
        {
            
        }

        public VoxelListener(ComponentManager manager, GameComponent parent, ChunkManager chunkManager, VoxelRef vref) :
            base(manager, "VoxelListener", parent)
        {
            Chunk = chunkManager.ChunkData.ChunkMap[vref.ChunkID];
            VoxelID = new Point3(vref.GridPosition);
            Chunk.OnVoxelDestroyed += VoxelListener_OnVoxelDestroyed;
            ChunkID = Chunk.ID;

        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Chunk = PlayState.ChunkManager.ChunkData.ChunkMap[ChunkID];
        }


        void VoxelListener_OnVoxelDestroyed(Point3 voxelID)
        {
            if(voxelID.Equals(VoxelID))
            {
                GetRootComponent().Die();
            }
        }

        public override void Die()
        {
            Chunk.OnVoxelDestroyed -= VoxelListener_OnVoxelDestroyed;
            base.Die();
        }

        public override void Delete()
        {
            Chunk.OnVoxelDestroyed -= VoxelListener_OnVoxelDestroyed;
            base.Delete();
        }
    }
}
