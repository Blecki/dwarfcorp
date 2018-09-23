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
        public ChunkData(int ChunkMapWidth, int ChunkMapHeight, int ChunkMapMinX, int ChunkMapMinZ)
        {           
            this.ChunkMapWidth = ChunkMapWidth;
            this.ChunkMapHeight = ChunkMapHeight;
            this.ChunkMapMinX = ChunkMapMinX;
            this.ChunkMapMinZ = ChunkMapMinZ;
            this.ChunkMap = new VoxelChunk[ChunkMapWidth * ChunkMapHeight];
        }

        // These have to be public so that VoxelHandle can access them effeciently. ;_;
        public VoxelChunk[] ChunkMap;
        public int ChunkMapWidth;
        public int ChunkMapHeight;
        public int ChunkMapMinX;
        public int ChunkMapMinZ;

        public VoxelChunk GetChunk(GlobalChunkCoordinate Coordinate)
        {
            if (!CheckBounds(Coordinate)) throw new IndexOutOfRangeException();
            return ChunkMap[(Coordinate.Z - ChunkMapMinZ) * ChunkMapWidth + (Coordinate.X - ChunkMapMinX)];
        }

        public bool CheckBounds(GlobalChunkCoordinate Coordinate)
        {
            if (Coordinate.X < ChunkMapMinX || Coordinate.X >= ChunkMapMinX + ChunkMapWidth) return false;
            if (Coordinate.Z < ChunkMapMinZ || Coordinate.Z >= ChunkMapMinZ + ChunkMapHeight) return false;
            if (Coordinate.Y != 0) return false;
            return true;
        }

        public GlobalChunkCoordinate ConfineToBounds(GlobalChunkCoordinate Coordinate)
        {
            var x = (Coordinate.X < ChunkMapMinX) ? ChunkMapMinX :
                (Coordinate.X >= (ChunkMapMinX + ChunkMapWidth) ? (ChunkMapMinX + ChunkMapWidth - 1) : Coordinate.X);
            var z = (Coordinate.Z < ChunkMapMinZ) ? ChunkMapMinZ :
                (Coordinate.Z >= (ChunkMapMinZ + ChunkMapHeight) ? (ChunkMapMinZ + ChunkMapHeight - 1) : Coordinate.Z);
            return new GlobalChunkCoordinate(x, 0, z);
        }

        public IEnumerable<VoxelChunk> GetChunkEnumerator()
        {
            return ChunkMap;
        }

        public bool AddChunk(VoxelChunk Chunk)
        {
            if (!CheckBounds(Chunk.ID)) throw new IndexOutOfRangeException();

            ChunkMap[(Chunk.ID.Z - ChunkMapMinZ) * ChunkMapWidth + (Chunk.ID.X - ChunkMapMinX)] = Chunk;
            return true;
        }

        public void LoadFromFile(ChunkManager Manager, SaveGame gameFile, Action<String> SetLoadingMessage)
        {
            if (gameFile.ChunkData.Count == 0)
            {
                throw new Exception("Game file corrupt. It has no chunk files.");
            }
            var maxChunkX = gameFile.ChunkData.Max(c => c.ID.X) + 1;
            var maxChunkZ = gameFile.ChunkData.Max(c => c.ID.Z) + 1;
            ChunkMapMinX = gameFile.ChunkData.Min(c => c.ID.X);
            ChunkMapMinZ = gameFile.ChunkData.Min(c => c.ID.Z);
            ChunkMapWidth = maxChunkX - ChunkMapMinX;
            ChunkMapHeight = maxChunkZ - ChunkMapMinZ;

            ChunkMap = new VoxelChunk[ChunkMapWidth * ChunkMapHeight];

            foreach (VoxelChunk chunk in gameFile.ChunkData.Select(file => file.ToChunk(Manager)))
                AddChunk(chunk);

            Manager.UpdateBounds();
        }
    }
}
