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
        public int MaxViewingLevel { get; set; }
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


        // Final argument is always mode Y.
        // Todo: %KILL% - does not belong here.
        public void SetMaxViewingLevel(int level, ChunkManager.SliceMode slice)
        {
            if (level == MaxViewingLevel && slice == Slice)
                return;

            var oldLevel = MaxViewingLevel;

            Slice = slice;
            MaxViewingLevel = Math.Max(Math.Min(level, VoxelConstants.ChunkSizeY), 1);

            foreach (VoxelChunk c in ChunkMap.Select(chunks => chunks.Value))
            {
                c.Data.SliceCache[oldLevel - 1] = null;
                c.Data.SliceCache[MaxViewingLevel - 1] = null;
                c.ShouldRecalculateLighting = true;
                c.ShouldRebuild = true;
            }
        }
        
        public bool AddChunk(VoxelChunk chunk)
        {
            if(ChunkMap.Count < MaxChunks && !ChunkMap.ContainsKey(chunk.ID))
            {
                ChunkMap[chunk.ID] = chunk;
                return true;
            }

            return false;
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
            foreach (var n in VoxelHelpers.EnumerateManhattanNeighbors2D(At))
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
