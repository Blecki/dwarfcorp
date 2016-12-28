// VoxelListener.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     When a voxel is destroyed, this component kills whatever it is attached to.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class VoxelListener : GameComponent
    {
        [JsonIgnore] public VoxelChunk Chunk;

        public bool DestroyOnTimer = false;
        public Point3 VoxelID;
        private bool firstIter;

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

        public Point3 ChunkID { get; set; }
        public Timer DestroyTimer { get; set; }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Chunk = PlayState.ChunkManager.ChunkData.ChunkMap[ChunkID];
            firstIter = true;
            Chunk.OnVoxelDestroyed += VoxelListener_OnVoxelDestroyed;
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
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


        private void VoxelListener_OnVoxelDestroyed(Point3 voxelID)
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

    [JsonObject(IsReference = true)]
    public class ExploredListener : GameComponent
    {
        [JsonIgnore] public VoxelChunk Chunk;
        public Point3 VoxelID;

        public ExploredListener()
        {
        }


        public ExploredListener(ComponentManager manager, GameComponent parent, ChunkManager chunkManager, Voxel vref) :
            base("ExploredListener", parent)
        {
            Chunk = vref.Chunk;
            VoxelID = new Point3(vref.GridPosition);
            Chunk.OnVoxelExplored += ExploredListener_OnVoxelExplored;
            ChunkID = Chunk.ID;
        }

        public Point3 ChunkID { get; set; }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Chunk = PlayState.ChunkManager.ChunkData.ChunkMap[ChunkID];
            Chunk.OnVoxelExplored += ExploredListener_OnVoxelExplored;
        }

        private void ExploredListener_OnVoxelExplored(Point3 voxelID)
        {
            if (voxelID.Equals(VoxelID))
            {
                GetRootComponent().SetActiveRecursive(true);
                GetRootComponent().SetVisibleRecursive(true);
                Delete();
            }
        }

        public override void Die()
        {
            Chunk.OnVoxelExplored -= ExploredListener_OnVoxelExplored;
            base.Die();
        }

        public override void Delete()
        {
            Chunk.OnVoxelExplored -= ExploredListener_OnVoxelExplored;
            base.Delete();
        }
    }
}