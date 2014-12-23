using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
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

        private bool firstIter = false;

        public bool DestroyOnTimer = false;
        public Timer DestroyTimer { get; set; }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Chunk = PlayState.ChunkManager.ChunkData.ChunkMap[ChunkID];
            firstIter = true;
            Chunk.OnVoxelDestroyed += VoxelListener_OnVoxelDestroyed;
        }

        public VoxelListener()
        {

        }


        public VoxelListener(ComponentManager manager, GameComponent parent, ChunkManager chunkManager, Voxel vref) :
            base("VoxelListener", parent)
        {
            Chunk = vref.Chunk;
            VoxelID = new Point3(vref.GridPosition);
            Chunk.OnVoxelDestroyed += VoxelListener_OnVoxelDestroyed;
            ChunkID = Chunk.ID;

        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (firstIter)
            {
                if (Chunk.Data.Types[Chunk.Data.IndexAt(VoxelID.X, VoxelID.Y, VoxelID.Z)] == 0)
                {
                    Delete();
                }
                firstIter = false;
            }

            if (DestroyOnTimer)
            {
                DestroyTimer.Update(gameTime);

                if (DestroyTimer.HasTriggered)
                {
                    Die();
                    Chunk.MakeVoxel(VoxelID.X, VoxelID.Y, VoxelID.Z).Kill();
                }
            }

            base.Update(gameTime, chunks, camera);
        }


        void VoxelListener_OnVoxelDestroyed(Point3 voxelID)
        {
            if (voxelID.Equals(VoxelID))
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
