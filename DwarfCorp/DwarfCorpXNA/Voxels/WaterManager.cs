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
    public enum LiquidType
    {
        None = 0,
        Water,
        Lava,
        Count
    }

    /// <summary>
    /// Handles the water simulation in the game.
    /// </summary>
    public class WaterManager
    {
        private ChunkManager Chunks { get; set; }
        private int[] updateList;
        private int[] randomIndices;
        private int maxChunks;
        public byte EvaporationLevel { get; set; }

        public static byte maxWaterLevel = 8;
        public static byte threeQuarterWaterLevel = 6;
        public static byte oneHalfWaterLevel = 4;
        public static byte oneQuarterWaterLevel = 2;
        public static byte rainFallAmount = 1;
        public static byte inWaterThreshold = 5;
        public static byte waterMoveThreshold = 1;
        
        private LinkedList<SplashType> Splashes = new LinkedList<SplashType>();
        private Mutex SplashLock = new Mutex();

        private LinkedList<Transfer> Transfers = new LinkedList<Transfer>();
        private Mutex TransferLock = new Mutex();

        public IEnumerable<SplashType> GetSplashQueue()
        {
            SplashLock.WaitOne();
            var r = Splashes;
            Splashes = new LinkedList<SplashType>();
            SplashLock.ReleaseMutex();
            return r;
        }

        public IEnumerable<Transfer> GetTransferQueue()
        {
            TransferLock.WaitOne();
            var r = Transfers;
            Transfers = new LinkedList<Transfer>();
            TransferLock.ReleaseMutex();
            return r;
        }

        public WaterManager(ChunkManager chunks)
        {
            Chunks = chunks;
            EvaporationLevel = 1;
            maxChunks = 81;
            ChunkData data = chunks.ChunkData;

            // Create reusable arrays for randomized indices
            updateList = new int[VoxelConstants.ChunkVoxelCount];
            randomIndices = new int[VoxelConstants.ChunkVoxelCount];
        }

        public void CreateTransfer(
            GlobalVoxelCoordinate worldPosition, WaterCell water1, WaterCell water2, byte amount)
        {
            Transfer transfer = new Transfer();
            transfer.amount = amount;
            transfer.cellFrom = water1;
            transfer.cellTo = water2;
            transfer.worldLocation = worldPosition;

            TransferLock.WaitOne();
            Transfers.AddFirst(transfer);
            TransferLock.ReleaseMutex();
        }

        public void CreateSplash(Vector3 pos, LiquidType liquid)
        {
            if (MathFunctions.RandEvent(0.9f)) return;

            SplashType splash;

            switch(liquid)
            {
                case LiquidType.Water:
                {
                    splash = new SplashType
                    {
                        name = "splash2",
                        numSplashes = 2,
                        position = pos,
                        sound = ContentPaths.Audio.river
                    };
                }
                    break;
                case LiquidType.Lava:
                {
                    splash = new SplashType
                    {
                        name = "flame",
                        numSplashes = 5,
                        position = pos,
                        sound = ContentPaths.Audio.fire
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
            {
                return;
            }

            List<VoxelChunk> chunksToUpdate = Chunks.ChunkData.ChunkMap.Select(chunks => chunks.Value).ToList();

            chunksToUpdate.Sort(Chunks.CompareChunkDistance);
            int chunksUpdated = 0;

            foreach(VoxelChunk chunk in chunksToUpdate)
            {
                if (chunksUpdated >= maxChunks)
                    continue;

                chunksUpdated++;

                if(!UpdateChunk(chunk) && !chunk.FirstWaterIter)
                {
                    chunk.FirstWaterIter = false;
                    continue;
                }

                chunk.ShouldRebuildWater = true;
                chunk.FirstWaterIter = false;
            }
        }

        public int CompareLevels(byte A, byte B)
        {
            if (A.Equals(B))
            {
                return 0;
            }
            else
            {
                if (A > B)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
        }

        private int CompareFlowVectors(Vector3 A, Vector3 B, Vector3 flow)
        {
            if (A.Equals(B))
            {
                return 0;
            }
            else
            {
                float dotA = Vector3.Dot(A, flow);
                float dotB = Vector3.Dot(B, flow);

                if (dotA > dotB)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
        }

        private static Vector3 CoordsAt(int idx)
        {
            int x = idx % (VoxelConstants.ChunkSizeX);
            idx /= (VoxelConstants.ChunkSizeX);
            int y = idx % (VoxelConstants.ChunkSizeY);
            idx /= (VoxelConstants.ChunkSizeY);
            int z = idx;
            return new Vector3(x, y, z);
        }

        public bool DiscreteUpdate(VoxelChunk chunk)
        {
            var gridCoord = Vector3.Zero;
            bool updateOccurred = false;
            WaterCell cellBelow = new WaterCell();

            int maxSize = VoxelConstants.ChunkVoxelCount;
            int toUpdate = 0;
            var data = chunk.Data;
            for (int i = 0; i < maxSize; i++)
            {
                if (data.Types[i] != 0)
                {
                    data.Water[i].WaterLevel = 0;
                    data.Water[i].Type = LiquidType.None;
                    continue;
                }
                // Don't check empty cells or cells we've already modified.
                if (data.Water[i].WaterLevel < 1)
                {
                    continue;
                }
                updateList[toUpdate] = i;
                toUpdate++;
            }

            if (toUpdate == 0)
            {
                return false;
            }

            TemporaryVoxelHandle voxBelow = TemporaryVoxelHandle.InvalidHandle;

            for (int i = 0; i < toUpdate; i++)
            {
                randomIndices[i] = i;
            }
            randomIndices.ShuffleSeedRandom(toUpdate);

            // Loop through each cell.                
            for (int t = 0; t < toUpdate; t++)
            {
                int idx = updateList[randomIndices[t]];

                // Don't check empty cells or cells we've already modified.
                if (data.Water[idx].Type == LiquidType.None || data.Types[idx] != 0)
                {
                    continue;
                }

                //Todo: %KILL% the CoordsAt bit.
                gridCoord = CoordsAt(idx);
                int x = (int)gridCoord.X;
                int y = (int)gridCoord.Y;
                int z = (int)gridCoord.Z;
                Vector3 worldPos = gridCoord + chunk.Origin;

                if (data.Water[idx].WaterLevel <= EvaporationLevel && MathFunctions.RandEvent(0.01f))
                {
                    data.Water[idx].WaterLevel = 0;

                    if (data.Water[idx].Type == LiquidType.Lava)
                    {
                        data.Types[idx] = (byte)VoxelLibrary.GetVoxelType("Stone").ID;
                        data.Health[idx] = (byte)VoxelLibrary.GetVoxelType("Stone").StartingHealth;
                        chunk.ShouldRebuild = true;
                        chunk.ShouldRecalculateLighting = true;
                    }
                    data.Water[idx].Type = LiquidType.None;

                    updateOccurred = true;
                }

                bool shouldFall = false;

                // Now check the cell immediately below this one.
                // There are two cases, either we are at the bottom of the chunk,
                // in which case we must find the water from the chunk manager.
                // Otherwise, we just get the cell immediately beneath us.
                if (y > 0)
                {
                    voxBelow = new TemporaryVoxelHandle(chunk, new LocalVoxelCoordinate(x, y - 1, z));
                    if (voxBelow.IsEmpty)
                    {
                        cellBelow = voxBelow.WaterCell;
                        shouldFall = true;
                    }
                }
                /*  This is commented out as Chunks take up the full vertical space.  This needs to be added/fixed if the chunks take up less space.
                else
                {
                    if(chunk.Manager.ChunkData.DoesWaterCellExist(worldPos))
                    {
                        DestinationVoxel voxelsBelow = chunk.Manager.ChunkData.GetVoxel(chunk, worldPos + new Vector3(0, -1, 0));

                        if(voxelsBelow != null && voxelsBelow.IsEmpty)
                        {
                            cellBelow = chunk.Manager.ChunkData.GetWaterCellAtLocation(worldPos + new Vector3(0, -1, 0));
                            shouldFall = true;
                            cellBelow.IsFalling = true;
                        }
                    }
                }
                 */

                // Cases where the fluid can fall down.
                if (shouldFall)
                {
                    // If the cell immediately below us is empty,
                    // swap the contents and move on.
                    if (cellBelow.WaterLevel < 1)
                    {
                        CreateSplash(worldPos, data.Water[idx].Type);

                        cellBelow.WaterLevel = data.Water[idx].WaterLevel;
                        if (cellBelow.Type == LiquidType.None)
                        {
                            cellBelow.Type = data.Water[idx].Type;
                        }
                        data.Water[idx].WaterLevel = 0;
                        data.Water[idx].Type = LiquidType.None;
                        voxBelow.WaterCell = cellBelow;
                        CreateTransfer(GlobalVoxelCoordinate.FromVector3(worldPos), data.Water[idx], cellBelow, cellBelow.WaterLevel);
                        updateOccurred = true;

                        continue;
                    }
                    // Otherwise, fill as much of the space as we can.
                    else
                    {
                        byte spaceLeft = (byte)(maxWaterLevel - cellBelow.WaterLevel);

                        if (spaceLeft > 0)
                        {
                            // Special case where we can flow completely into the next cell.
                            if (spaceLeft >= data.Water[idx].WaterLevel)
                            {
                                byte transfer = data.Water[idx].WaterLevel;
                                cellBelow.WaterLevel += transfer;
                                if (cellBelow.Type == LiquidType.None)
                                {
                                    cellBelow.Type = data.Water[idx].Type;
                                }
                                data.Water[idx].WaterLevel = 0;
                                data.Water[idx].Type = LiquidType.None;

                                CreateTransfer(GlobalVoxelCoordinate.FromVector3(worldPos - Vector3.UnitY), data.Water[idx], cellBelow, transfer);
                                voxBelow.WaterCell = cellBelow;
                                updateOccurred = true;
                                continue;
                            }
                            // Otherwise, only flow a little bit, and spread later.
                            else
                            {
                                data.Water[idx].WaterLevel -= spaceLeft;
                                cellBelow.WaterLevel += spaceLeft;
                                if (cellBelow.Type == LiquidType.None)
                                {
                                    cellBelow.Type = data.Water[idx].Type;
                                }
                                CreateTransfer(GlobalVoxelCoordinate.FromVector3(worldPos - Vector3.UnitY), data.Water[idx], cellBelow, spaceLeft);
                                voxBelow.WaterCell = cellBelow;
                            }
                        }
                    }
                }

                // Now the only fluid left can spread.
                // We spread to the manhattan neighbors
                //Array.Sort(m_spreadNeighbors, (a, b) => CompareFlowVectors(a, b, data.Water[idx].FluidFlow));
                //m_spreadNeighbors.Shuffle();
                
                foreach (var globalCoordinate in VoxelHelpers.EnumerateManhattanNeighbors2D(
                        chunk.ID + new LocalVoxelCoordinate(x, y, z)))
                {
                    var v = new TemporaryVoxelHandle(chunk.Manager.ChunkData, globalCoordinate);
                    if (!v.IsValid || !v.IsEmpty) continue;
                    var neighborWater = v.WaterCell;
                    
                    if (neighborWater.WaterLevel >= data.Water[idx].WaterLevel)
                        continue;

                    byte amountToMove = (byte)(
                        Math.Min(maxWaterLevel - neighborWater.WaterLevel, data.Water[idx].WaterLevel) * GetSpreadRate(data.Water[idx].Type)
                    );

                    if (amountToMove == 0)
                    {
                        continue;
                    }

                    if (neighborWater.WaterLevel < oneQuarterWaterLevel)
                    {
                        updateOccurred = true;
                    }

                    CreateTransfer(globalCoordinate, data.Water[idx], neighborWater, amountToMove);

                    data.Water[idx].WaterLevel -= amountToMove;
                    neighborWater.WaterLevel += amountToMove;

                    if (neighborWater.Type == LiquidType.None)
                    {
                        neighborWater.Type = data.Water[idx].Type;
                    }

                    v.WaterCell = neighborWater;

                    if (data.Water[idx].WaterLevel >= 1)
                    {
                        continue;
                    }

                    data.Water[idx].WaterLevel = 0;
                    data.Water[idx].Type = LiquidType.None;
                    break;
                }
            }

            return updateOccurred;
        }

        public bool UpdateChunk(VoxelChunk chunk)
        {
            return DiscreteUpdate(chunk);
        }
    }

}
