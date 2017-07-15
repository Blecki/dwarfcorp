// ChunkData.cs
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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// Has a collection of voxel chunks, and methods for accessing them.
    /// </summary>
    public class ChunkData
    {
        private ChunkManager chunkManager;

        public ChunkData(ChunkManager chunkManager)
        {           
            this.chunkManager = chunkManager;
            ChunkMap = new ConcurrentDictionary<GlobalChunkCoordinate, VoxelChunk>();
            MaxViewingLevel = VoxelConstants.ChunkSizeY;
            Slice = ChunkManager.SliceMode.Y;
        }

        public ConcurrentDictionary<GlobalChunkCoordinate, VoxelChunk> ChunkMap { get; set; }

        public Texture2D Tilemap
        {
            get { return TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles); }
        }

        public Texture2D IllumMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Terrain.terrain_illumination); }
        }

        public int MaxChunks
        {
            get { return (int) GameSettings.Default.MaxChunks; }
            set { GameSettings.Default.MaxChunks = (int) value; }
        }

        // Todo: Why is this here?
        public float MaxViewingLevel { get; set; }
        public ChunkManager.SliceMode Slice { get; set; }

        public Texture2D SunMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Gradients.sungradient); }
        }

        public Texture2D AmbientMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Gradients.ambientgradient); }
        }

        public Texture2D TorchMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Gradients.torchgradient); }
        }

        public ChunkManager ChunkManager
        {
            set { chunkManager = value; }
            get { return chunkManager; }
        }

        public void SetMaxViewingLevel(float level, ChunkManager.SliceMode slice)
        {
            if (Math.Abs(level - MaxViewingLevel) < 0.1f && slice == Slice)
            {
                return;
            }

            Slice = slice;
            MaxViewingLevel = Math.Max(Math.Min(level, VoxelConstants.ChunkSizeY), 1);

            foreach (VoxelChunk c in ChunkMap.Select(chunks => chunks.Value))
            {
                c.ShouldRecalculateLighting = true;
                c.ShouldRebuild = true;
            }
        }

        public void Reveal(VoxelHandle voxel)
        {
            Reveal(new List<VoxelHandle>() {voxel});
        }

        public void Reveal(IEnumerable<VoxelHandle> voxels)
        {
            if (!GameSettings.Default.FogofWar) return;

            var queue = new Queue<TemporaryVoxelHandle>(128);

            foreach (VoxelHandle voxel in voxels)
            {
                if (voxel != null)
                    queue.Enqueue(new TemporaryVoxelHandle(this, voxel.Coordinate));
            }

            while (queue.Count > 0)
            {
                var v = queue.Dequeue();
                if (!v.IsValid) continue;

                foreach (var neighborCoordinate in Neighbors.EnumerateManhattanNeighbors(v.Coordinate))
                {
                    var neighbor = new TemporaryVoxelHandle(this, neighborCoordinate);
                    if (!neighbor.IsValid) continue;
                    if (neighbor.IsExplored) continue;
                    neighbor.Chunk.NotifyExplored(neighbor.Coordinate.GetLocalVoxelCoordinate());
                    neighbor.IsExplored = true;
                    if (neighbor.IsEmpty)
                        queue.Enqueue(neighbor);

                    if (!neighbor.Chunk.ShouldRebuild)
                    {
                        neighbor.Chunk.ShouldRebuild = true;
                        neighbor.Chunk.ShouldRebuildWater = true;
                        neighbor.Chunk.ShouldRecalculateLighting = true;
                    }
                }

                v.IsExplored = true;
            }
        }
        
        public bool AddChunk(VoxelChunk chunk)
        {
            if(ChunkMap.Count < MaxChunks && !ChunkMap.ContainsKey(chunk.ID))
            {
                ChunkMap[chunk.ID] = chunk;
                return true;
            }
            else if(ChunkMap.ContainsKey(chunk.ID))
            {
                return false;
            }

            return false;
        }

        public VoxelChunk GetVoxelChunkAtWorldLocation(GlobalVoxelCoordinate worldLocation)
        {
            VoxelChunk returnChunk = null;
            ChunkMap.TryGetValue(worldLocation.GetGlobalChunkCoordinate(), out returnChunk);
            return returnChunk;
        }

        [Obsolete]
        public VoxelChunk GetChunk(Vector3 WorldLocation)
        {
            return GetVoxelChunkAtWorldLocation(new GlobalVoxelCoordinate(
                (int)Math.Floor(WorldLocation.X),
                (int)Math.Floor(WorldLocation.Y),
                (int)Math.Floor(WorldLocation.Z)));
        }

        [Obsolete]
        public bool GetVoxel(Vector3 WorldLocation, ref VoxelHandle Voxel)
        {
            return GetVoxel(null, new GlobalVoxelCoordinate(
                (int)Math.Floor(WorldLocation.X),
                (int)Math.Floor(WorldLocation.Y),
                (int)Math.Floor(WorldLocation.Z)), ref Voxel);
        }

        [Obsolete]
        public bool GetVoxel(GlobalVoxelCoordinate worldLocation, ref VoxelHandle voxel)
        {
            return GetVoxel(null, worldLocation, ref voxel);
        }

        [Obsolete]
        public bool GetVoxel(VoxelChunk checkFirst, GlobalVoxelCoordinate worldLocation, ref VoxelHandle newReference)
        {
            var chunk = GetVoxelChunkAtWorldLocation(worldLocation);
            if (chunk == null) return false;
            return chunk.GetVoxelAtValidWorldLocation(worldLocation, ref newReference);
        }
        
        /// <summary> 
        /// Given a world location, returns the voxel at that location if it exists
        /// Otherwise returns null.
        /// </summary>
        /// <param Name="worldLocation">A floating point vector location in the world space</param>
        /// <param Name="depth">unused</param>
        /// <returns>The voxel at that location (as a list)</returns>
        [Obsolete]
        public bool GetNonNullVoxelAtWorldLocation(Vector3 worldLocation, ref VoxelHandle voxel)
        {
            return GetNonNullVoxelAtWorldLocationCheckFirst(null, worldLocation, ref voxel);
        }

        /// <summary>
        /// Recursive function which gets all the voxels at a position in the world, assuming the voxel is in a given chunk
        /// </summary>
        /// <param Name="checkFirst">The voxel chunk to check first</param>
        /// <param Name="worldLocation">The point in the world to check</param>
        /// <param Name="toReturn">A list of voxels to get</param>
        public bool GetNonNullVoxelAtWorldLocationCheckFirst(VoxelChunk checkFirst, Vector3 worldLocation, ref VoxelHandle toReturn)
        {
            var v = new TemporaryVoxelHandle(this, GlobalVoxelCoordinate.FromVector3(worldLocation));
            if (!v.IsValid || v.IsEmpty) return false;
            toReturn.ChangeVoxel(v.Chunk, v.Coordinate.GetLocalVoxelCoordinate());
            return true;
        }
       
        public void LoadFromFile(GameFile gameFile, Action<String> SetLoadingMessage)
        {
            foreach (VoxelChunk chunk in gameFile.Data.ChunkData.Select(file => file.ToChunk(chunkManager)))
                AddChunk(chunk);

            chunkManager.UpdateBounds();
            chunkManager.CreateGraphics(SetLoadingMessage, this);
        }

        public void NotifyRebuild(GlobalVoxelCoordinate At)
        {
            foreach (var n in Neighbors.EnumerateManhattanNeighbors2D(At))
            {
                var vox = new TemporaryVoxelHandle(this, n);
                if (vox.IsValid)
                {
                    vox.Chunk.ShouldRebuild = true;
                    vox.Chunk.ShouldRecalculateLighting = true;
                    vox.Chunk.ShouldRebuildWater = true;
                    vox.Chunk.ReconstructRamps = true;
                }
            }
        }
    }
}
