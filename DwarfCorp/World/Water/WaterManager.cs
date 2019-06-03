// WaterManager.cs
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Collections.Concurrent;
using System.Threading;

namespace DwarfCorp
{
    /// <summary>
    /// Handles the water simulation in the game.
    /// </summary>
    public class WaterManager
    {
        private ChunkManager Chunks { get; set; }
        public byte EvaporationLevel { get; set; }

        public static byte maxWaterLevel = 8;
        public static byte threeQuarterWaterLevel = 6;
        public static byte oneHalfWaterLevel = 4;
        public static byte oneQuarterWaterLevel = 2;
        public static byte rainFallAmount = 1;
        public static byte inWaterThreshold = 5;
        public static byte waterMoveThreshold = 1;

        private int[][] SlicePermutations;
        private int[][] NeighborPermutations = new int[][]
        {
            new int[] { 0, 1, 2, 3 },
            new int[] { 0, 1, 3, 2 },
            new int[] { 0, 2, 1, 3 },
            new int[] { 0, 2, 3, 1 },
            new int[] { 0, 3, 1, 2 },
            new int[] { 0, 3, 2, 1 }
        };

        private int[] NeighborScratch = new int[4];

        private void RollArray(int[] from, int[] into, int offset)
        {
            for (var i = 0; i < 4; ++i)
            {
                into[offset] = from[i];
                offset = (offset + 1) & 0x3;
            }
        }
                
        private LinkedList<LiquidSplash> Splashes = new LinkedList<LiquidSplash>();
        private Mutex SplashLock = new Mutex();
        public bool NeedsMinimapUpdate = true;

        public IEnumerable<LiquidSplash> GetSplashQueue()
        {
            SplashLock.WaitOne();
            var r = Splashes;
            Splashes = new LinkedList<LiquidSplash>();
            SplashLock.ReleaseMutex();
            return r;
        }

        public WaterManager(ChunkManager chunks)
        {
            Chunks = chunks;
            EvaporationLevel = 1;

            // Create permutation arrays for random update orders.
            SlicePermutations = new int[16][];
            var temp = new int[VoxelConstants.ChunkSizeX * VoxelConstants.ChunkSizeZ];
            for (var i = 0; i < temp.Length; ++i)
                temp[i] = i;
            for (var i = 0; i < 16; ++i)
            {
                temp.Shuffle();
                SlicePermutations[i] = temp.ToArray(); // Copies the array
            }
        }

        public void HandleLiquidInteraction(VoxelHandle Vox, LiquidType From, LiquidType To)
        {
            if ((From == LiquidType.Lava && To == LiquidType.Water)
                || (From == LiquidType.Water && To == LiquidType.Lava))
            {
                Vox.Type = Library.GetVoxelType("Stone");
                Vox.QuickSetLiquid(LiquidType.None, 0);
            }            
        }

        public void CreateSplash(Vector3 pos, LiquidType liquid)
        {
            if (MathFunctions.RandEvent(0.25f)) return;

            LiquidSplash splash;

            switch(liquid)
            {
                case LiquidType.Water:
                {
                    splash = new LiquidSplash
                    {
                        name = "splat",
                        numSplashes = 2,
                        position = pos,
                        sound = ContentPaths.Audio.river
                    };
                }
                    break;
                case LiquidType.Lava:
                {
                    splash = new LiquidSplash
                    {
                        name = "flame",
                        numSplashes = 5,
                        position = pos,
                        sound = ContentPaths.Audio.Oscar.sfx_env_lava_spread,
                        entity =  "Fire"
                    };
                }
                    break;
                default:
                    throw new InvalidOperationException();
            }

            SplashLock.WaitOne();
            Splashes.AddFirst(splash);
            SplashLock.ReleaseMutex();
        }

        public float GetSpreadRate(LiquidType type)
        {
            switch (type)
            {
                case LiquidType.Lava:
                    return 0.1f + MathFunctions.Rand() * 0.1f;
                case LiquidType.Water:
                    return 0.5f;
            }

            return 1.0f;
        }

        public void UpdateWater()
        {
            if(Chunks.World.Paused)
                return;
            
            foreach(var chunk in Chunks.GetChunkEnumerator())
            {
                DiscreteUpdate(Chunks, chunk);
                chunk.RebuildLiquids();
            }
        }

        private void DiscreteUpdate(ChunkManager ChunkManager, VoxelChunk chunk)
        {
            for (var y = 0; y < VoxelConstants.ChunkSizeY; ++y)
            {
                // Apply 'liquid present' tracking in voxel data to skip entire slices.
                if (chunk.Data.LiquidPresent[y] == 0) continue;

                var layerOrder = SlicePermutations[MathFunctions.RandInt(0, SlicePermutations.Length)];

                for (var i = 0; i < layerOrder.Length; ++i)
                {
                    var x = layerOrder[i] % VoxelConstants.ChunkSizeX;
                    var z = (layerOrder[i] >> VoxelConstants.XDivShift) % VoxelConstants.ChunkSizeZ;
                    var currentVoxel = VoxelHandle.UnsafeCreateLocalHandle(chunk, new LocalVoxelCoordinate(x, y, z));

                    if (currentVoxel.TypeID != 0)
                        continue;

                    if (currentVoxel.LiquidType == LiquidType.None || currentVoxel.LiquidLevel < 1)
                        continue;

                    // Evaporate.
                    if (currentVoxel.LiquidLevel <= EvaporationLevel && MathFunctions.RandEvent(0.01f))
                    {
                        if (currentVoxel.LiquidType == LiquidType.Lava)
                            currentVoxel.Type = Library.GetVoxelType("Stone");

                        NeedsMinimapUpdate = true;
                        currentVoxel.QuickSetLiquid(LiquidType.None, 0);
                        continue;
                    }

                    var voxBelow = ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(currentVoxel.Coordinate.X, currentVoxel.Coordinate.Y - 1, currentVoxel.Coordinate.Z));

                    if (voxBelow.IsValid && voxBelow.IsEmpty)
                    {
                        // Fall into the voxel below.

                        // Special case: No liquid below, just drop down.
                        if (voxBelow.LiquidType == LiquidType.None)
                        {
                            NeedsMinimapUpdate = true;
                            CreateSplash(currentVoxel.Coordinate.ToVector3(), currentVoxel.LiquidType);
                            voxBelow.QuickSetLiquid(currentVoxel.LiquidType, currentVoxel.LiquidLevel);
                            currentVoxel.QuickSetLiquid(LiquidType.None, 0);
                            continue;
                        }

                        var belowType = voxBelow.LiquidType;
                        var aboveType = currentVoxel.LiquidType;
                        var spaceLeftBelow = maxWaterLevel - voxBelow.LiquidLevel;

                        if (spaceLeftBelow >= currentVoxel.LiquidLevel)
                        {
                            NeedsMinimapUpdate = true;
                            CreateSplash(currentVoxel.Coordinate.ToVector3(), aboveType);
                            voxBelow.LiquidLevel += currentVoxel.LiquidLevel;
                            currentVoxel.QuickSetLiquid(LiquidType.None, 0);
                            HandleLiquidInteraction(voxBelow, aboveType, belowType);
                            continue;
                        }

                        if (spaceLeftBelow > 0)
                        {
                            NeedsMinimapUpdate = true;
                            CreateSplash(currentVoxel.Coordinate.ToVector3(), aboveType);
                            currentVoxel.LiquidLevel = (byte)(currentVoxel.LiquidLevel - maxWaterLevel + voxBelow.LiquidLevel);
                            voxBelow.LiquidLevel = maxWaterLevel;
                            HandleLiquidInteraction(voxBelow, aboveType, belowType);
                            continue;
                        }
                    }
                    else if (voxBelow.IsValid && currentVoxel.LiquidType == LiquidType.Lava && !voxBelow.IsEmpty && voxBelow.GrassType > 0)
                    {
                        voxBelow.GrassType = 0;
                    }

                    if (currentVoxel.LiquidLevel <= 1) continue;

                    // Nothing left to do but spread.

                    RollArray(NeighborPermutations[MathFunctions.RandInt(0, NeighborPermutations.Length)], NeighborScratch, MathFunctions.RandInt(0, 4));

                    for (var n = 0; n < NeighborScratch.Length; ++n)
                    {
                        var neighborOffset = VoxelHelpers.ManhattanNeighbors2D[NeighborScratch[n]];
                        var neighborVoxel = new VoxelHandle(Chunks, currentVoxel.Coordinate + neighborOffset);

                        if (neighborVoxel.IsValid && neighborVoxel.IsEmpty)
                        {
                            if (neighborVoxel.LiquidLevel < currentVoxel.LiquidLevel)
                            {
                                NeedsMinimapUpdate = true;
                                var amountToMove = (int)(currentVoxel.LiquidLevel * GetSpreadRate(currentVoxel.LiquidType));
                                if (neighborVoxel.LiquidLevel + amountToMove > maxWaterLevel)
                                    amountToMove = maxWaterLevel - neighborVoxel.LiquidLevel;

                                if (amountToMove > 2)
                                {
                                    CreateSplash(neighborVoxel.Coordinate.ToVector3(), currentVoxel.LiquidType);
                                }

                                var newWater = currentVoxel.LiquidLevel - amountToMove;

                                var sourceType = currentVoxel.LiquidType;
                                var destType = neighborVoxel.LiquidType;
                                currentVoxel.QuickSetLiquid(newWater == 0 ? LiquidType.None : sourceType, (byte)newWater);
                                neighborVoxel.QuickSetLiquid(destType == LiquidType.None ? sourceType : destType, (byte)(neighborVoxel.LiquidLevel + amountToMove));
                                HandleLiquidInteraction(neighborVoxel, sourceType, destType);
                                break; 
                            }

                        }
                    }
                }
            }
        }
    }
}
