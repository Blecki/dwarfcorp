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
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp
{


    public enum LiquidType
    {
        None,
        Water,
        Lava
    }

    /// <summary>
    /// Handles the water simulation in the game.
    /// </summary>
    public class WaterManager
    {
        private Dictionary<string, Timer> splashNoiseLimiter = new Dictionary<string, Timer>();
        private ChunkManager Chunks { get; set; }
        public byte EvaporationLevel { get; set; }

        public struct Transfer
        {
            public WaterCell cellFrom;
            public WaterCell cellTo;
            public byte amount;
            public Vector3 worldLocation;
        }

        public struct SplashType
        {
            public string name;
            public Vector3 position;
            public int numSplashes;
            public string sound;
        }

        public ConcurrentQueue<SplashType> Splashes { get; set; }
        public ConcurrentQueue<Transfer> Transfers { get; set; }

        public static Vector3[] m_spreadNeighbors =
        {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1)
        };

        public WaterManager(ChunkManager chunks)
        {
            Chunks = chunks;
            EvaporationLevel = 1;
            Splashes = new ConcurrentQueue<SplashType>();
            Transfers = new ConcurrentQueue<Transfer>();
            splashNoiseLimiter["splash2"] = new Timer(0.1f, false);
            splashNoiseLimiter["flame"] = new Timer(0.1f, false);
        }

        public void CreateTransfer(Vector3 worldPosition, WaterCell water1, WaterCell water2, byte amount)
        {
            Transfer transfer = new Transfer();
            transfer.amount = amount;
            transfer.cellFrom = water1;
            transfer.cellTo = water2;
            transfer.worldLocation = worldPosition;

            Transfers.Enqueue(transfer);
        }

        public void CreateSplash(Vector3 pos, LiquidType liquid)
        {
            if (MathFunctions.RandEvent(0.9f)) return;
            switch(liquid)
            {
                case LiquidType.Water:
                {
                    SplashType splash = new SplashType
                    {
                        name = "splash2",
                        numSplashes = 2,
                        position = pos,
                        sound = ContentPaths.Audio.river
                    };
                    Splashes.Enqueue(splash);
                }
                    break;
                case LiquidType.Lava:
                {
                    SplashType splash = new SplashType
                    {
                        name = "flame",
                        numSplashes = 5,
                        position = pos,
                        sound = ContentPaths.Audio.fire
                    };
                    Splashes.Enqueue(splash);
                }
                    break;
            }
        }

        public void HandleTransfers(DwarfTime time)
        {
            Voxel atPos = new Voxel();
            while(Transfers.Count > 0)
            {
                Transfer transfer;

                if(!Transfers.TryDequeue(out transfer))
                {
                    break;
                }

                if((transfer.cellFrom.Type == LiquidType.Lava 
                && transfer.cellTo.Type == LiquidType.Water) || 
                (transfer.cellFrom.Type == LiquidType.Water && transfer.cellTo.Type == LiquidType.Lava))
                {
                    bool success = Chunks.ChunkData.GetVoxel(transfer.worldLocation, ref atPos);

                    if(success)
                    {
                        Voxel v = atPos;

                        VoxelLibrary.PlaceType(VoxelLibrary.GetVoxelType("Stone"), v);
                        VoxelChunk chunk = Chunks.ChunkData.ChunkMap[v.ChunkID];
                        chunk.Data.Water[v.Index].Type = LiquidType.None;
                        chunk.Data.Water[v.Index].WaterLevel = 0;
                        chunk.ShouldRebuild = true;
                        chunk.ShouldRecalculateLighting = true;
                    }
                }
            }
        }

        public void Splash(DwarfTime time)
        {
            while(Splashes.Count > 0)
            {
                SplashType splash;

                if(!Splashes.TryDequeue(out splash))
                {
                    break;
                }

                WorldManager.ParticleManager.Trigger(splash.name, splash.position + new Vector3(0.5f, 0.5f, 0.5f), Color.White, splash.numSplashes);

                if(splashNoiseLimiter[splash.name].HasTriggered)
                {
                    SoundManager.PlaySound(splash.sound, splash.position + new Vector3(0.5f, 0.5f, 0.5f), true);
                }
            }

            foreach(Timer t in splashNoiseLimiter.Values)
            {
                t.Update(time);
            }
        }

        public float GetSpreadRate(LiquidType type)
        {
            switch(type)
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
            if(WorldManager.Paused)
            {
                return;
            }

            List<VoxelChunk> chunksToUpdate = Chunks.ChunkData.ChunkMap.Select(chunks => chunks.Value).ToList();

            chunksToUpdate.Sort(Chunks.CompareChunkDistance);

            foreach(VoxelChunk chunk in chunksToUpdate)
            {

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
            if(A.Equals(B))
            {
                return 0;
            }
            else
            {
                if(A > B)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
        }

        public int CompareFlowVectors(Vector3 A, Vector3 B, Vector3 flow)
        {
            if(A.Equals(B))
            {
                return 0;
            }
            else
            {
                float dotA = Vector3.Dot(A, flow);
                float dotB = Vector3.Dot(B, flow);

                if(dotA > dotB)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
        }

        public bool DiscreteUpdate(VoxelChunk chunk)
        {
            Vector3 gridCoord = new Vector3(0, 0, 0);

            bool updateOccurred = false;

            List<int> updateList = new List<int>();
            WaterCell cellBelow = new WaterCell();

            int maxSize = chunk.SizeX*chunk.SizeY*chunk.SizeZ;
            for (int i = 0; i < maxSize; i++)
            {
                WaterCell cell = chunk.Data.Water[i];
                // Don't check empty cells or cells we've already modified.
                if (cell.WaterLevel < 1 || chunk.Data.Types[i] != 0)
                {
                    continue;
                }
                updateList.Add(i);
            }

            if(updateList.Count == 0)
            {
                return false;
            }
            Voxel voxBelow = chunk.MakeVoxel(0, 0, 0);

            List<int> indices = Datastructures.RandomIndices(updateList.Count);

            // Loop through each cell.
            foreach(int t in indices)
            {
                int idx = updateList[indices[t]];
                

                // Don't check empty cells or cells we've already modified.
                if (chunk.Data.Water[idx].Type == LiquidType.None || chunk.Data.Types[idx] != 0)
                {
                    continue;
                }

                gridCoord = chunk.Data.CoordsAt(idx);
                int x = (int) gridCoord.X;
                int y = (int) gridCoord.Y;
                int z = (int) gridCoord.Z;
                Vector3 worldPos = gridCoord + chunk.Origin;

                if (chunk.Data.Water[idx].WaterLevel <= EvaporationLevel && MathFunctions.RandEvent(0.01f))
                {
                    if (chunk.Data.Water[idx].WaterLevel > 1)
                    {
                        chunk.Data.Water[idx].WaterLevel--;
                    }
                    else
                    {
                        chunk.Data.Water[idx].WaterLevel = 0;
                       
                        if (chunk.Data.Water[idx].Type == LiquidType.Lava)
                        {
                            chunk.Data.Types[idx] = (byte)VoxelLibrary.GetVoxelType("Stone").ID;
                            chunk.Data.Health[idx] = (byte)VoxelLibrary.GetVoxelType("Stone").StartingHealth;
                            chunk.ShouldRebuild = true;
                            chunk.ShouldRecalculateLighting = true;
                        }
                        chunk.Data.Water[idx].Type = LiquidType.None;
                    }
                    updateOccurred = true;
                }




                bool shouldFall = false;

 
                // Now check the cell immediately below this one.
                // There are two cases, either we are at the bottom of the chunk,
                // in which case we must find the water from the chunk manager.
                // Otherwise, we just get the cell immediately beneath us.
                if(y > 0)
                {
                    voxBelow.GridPosition = new Vector3(x, y - 1, z);
                    if(voxBelow.IsEmpty)
                    {
                        cellBelow = voxBelow.Water;
                        shouldFall = true;
                    }
                }
                    /*
                else
                {
                    if(chunk.Manager.ChunkData.DoesWaterCellExist(worldPos))
                    {
                        Voxel voxelsBelow = chunk.Manager.ChunkData.GetVoxel(chunk, worldPos + new Vector3(0, -1, 0));

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
                if(shouldFall)
                {
                    // If the cell immediately below us is empty,
                    // swap the contents and move on.
                    if(cellBelow.WaterLevel < 1)
                    {
                        CreateSplash(worldPos, chunk.Data.Water[idx].Type);
                        cellBelow.WaterLevel = chunk.Data.Water[idx].WaterLevel;
                        if(cellBelow.Type == LiquidType.None)
                        {
                            cellBelow.Type = chunk.Data.Water[idx].Type;
                        }
                        chunk.Data.Water[idx].WaterLevel = 0;
                        chunk.Data.Water[idx].Type = LiquidType.None;
                        voxBelow.Water = cellBelow;
                        CreateTransfer(worldPos, chunk.Data.Water[idx], cellBelow, cellBelow.WaterLevel);
                        updateOccurred = true;
                        
                        continue;
                    }
                        // Otherwise, fill as much of the space as we can.
                    else
                    {
                        byte spaceLeft = (byte) (8 - cellBelow.WaterLevel);


                        // Special case where we can flow completely into the next cell.
                        if (spaceLeft >= chunk.Data.Water[idx].WaterLevel)
                        {
                            byte transfer = chunk.Data.Water[idx].WaterLevel;
                            cellBelow.WaterLevel += transfer;
                            if(cellBelow.Type == LiquidType.None)
                            {
                                cellBelow.Type = chunk.Data.Water[idx].Type;
                            }
                            chunk.Data.Water[idx].WaterLevel = 0;
                            chunk.Data.Water[idx].Type = LiquidType.None;

                            CreateTransfer(worldPos - Vector3.UnitY, chunk.Data.Water[idx], cellBelow, transfer);
                            voxBelow.Water = cellBelow;
                            updateOccurred = true;
                            continue;
                        }
                            // Otherwise, only flow a little bit, and spread later.
                        else
                        {
                            chunk.Data.Water[idx].WaterLevel -= spaceLeft;
                            cellBelow.WaterLevel += spaceLeft;
                            if(cellBelow.Type == LiquidType.None)
                            {
                                cellBelow.Type = chunk.Data.Water[idx].Type;
                            }
                            CreateTransfer(worldPos - Vector3.UnitY, chunk.Data.Water[idx], cellBelow, spaceLeft);
                            voxBelow.Water = cellBelow;
                        }
                    }
                }

                // Now the only fluid left can spread.
                // We spread to the manhattan neighbors
                //Array.Sort(m_spreadNeighbors, (a, b) => CompareFlowVectors(a, b, chunk.Data.Water[idx].FluidFlow));
                m_spreadNeighbors.Shuffle();
                Voxel neighbor = new Voxel();
                foreach(Vector3 spread in m_spreadNeighbors)
                {
                    bool success = chunk.Manager.ChunkData.GetVoxel(chunk, worldPos + spread, ref neighbor);

                    if(!success)
                    {
                        continue;
                    }

                    if(!neighbor.IsEmpty)
                    {
                        continue;
                    }

                    WaterCell neighborWater = neighbor.Water;

                    if (neighborWater.WaterLevel >= chunk.Data.Water[idx].WaterLevel) continue;

                    byte amountToMove = (byte)(Math.Min(8.0f - (float)neighborWater.WaterLevel, chunk.Data.Water[idx].WaterLevel) * GetSpreadRate(chunk.Data.Water[idx].Type));

                    if(amountToMove == 0)
                    {
                        continue;
                    }


                    if(neighborWater.WaterLevel < 2)
                    {
                        updateOccurred = true;
                    }

                    CreateTransfer(worldPos + spread, chunk.Data.Water[idx], neighborWater, amountToMove);

                    chunk.Data.Water[idx].WaterLevel -= amountToMove;
                    neighborWater.WaterLevel += amountToMove;

                    if(neighborWater.Type == LiquidType.None)
                    {
                        neighborWater.Type = chunk.Data.Water[idx].Type;
                    }

                    neighbor.Water = neighborWater;

                    if (chunk.Data.Water[idx].WaterLevel >= 1)
                    {
                        continue;
                    }

                    chunk.Data.Water[idx].WaterLevel = 0;
                    chunk.Data.Water[idx].Type = LiquidType.None;
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