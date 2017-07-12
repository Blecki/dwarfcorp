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
        private Dictionary<string, Timer> splashNoiseLimiter = new Dictionary<string, Timer>();
        private ChunkManager Chunks { get; set; }
        private int[] updateList;
        private int[] randomIndices;
        public byte EvaporationLevel { get; set; }

        public static byte maxWaterLevel = 8;
        public static byte threeQuarterWaterLevel;
        public static byte oneHalfWaterLevel;
        public static byte oneQuarterWaterLevel;
        public static byte rainFallAmount;
        public static byte inWaterThreshold;
        public static byte waterMoveThreshold = 1;

        public struct Transfer
        {
            public WaterCell cellFrom;
            public WaterCell cellTo;
            public byte amount;
            public GlobalVoxelCoordinate worldLocation;
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

        // Todo: %KILL%
        public static GlobalVoxelOffset[] m_spreadNeighbors =
        {
            new GlobalVoxelOffset(1, 0, 0),
            new GlobalVoxelOffset(-1, 0, 0),
            new GlobalVoxelOffset(0, 0, 1),
            new GlobalVoxelOffset(0, 0, -1)
        };

        public WaterManager(ChunkManager chunks)
        {
            Chunks = chunks;
            EvaporationLevel = 1;
            ChunkData data = chunks.ChunkData;

            // Create reusable arrays for randomized indices
            uint maxSize = data.ChunkSizeX * data.ChunkSizeY * data.ChunkSizeZ;
            updateList = new int[maxSize];
            randomIndices = new int[maxSize];

            Splashes = new ConcurrentQueue<SplashType>();
            Transfers = new ConcurrentQueue<Transfer>();
            splashNoiseLimiter["splash2"] = new Timer(0.1f, false);
            splashNoiseLimiter["flame"] = new Timer(0.1f, false);
            InitializeStatics();
        }

        public void InitializeStatics()
        {
            float oneEighth = maxWaterLevel / 8;

            threeQuarterWaterLevel = (byte)(Math.Round(oneEighth * 6));
            oneHalfWaterLevel = (byte)(Math.Round(oneEighth * 4));
            oneQuarterWaterLevel = (byte)(Math.Round(oneEighth * 2));
            rainFallAmount = (byte)(Math.Round(oneEighth));
            inWaterThreshold = (byte)(Math.Round(oneEighth * 5));
            waterMoveThreshold = 1;
        }

        public void CreateTransfer(GlobalVoxelCoordinate worldPosition, WaterCell water1, WaterCell water2, byte amount)
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
            VoxelHandle atPos = new VoxelHandle();
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
                        VoxelHandle v = atPos;

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
            while (Splashes.Count > 0)
            {
                SplashType splash;

                if (!Splashes.TryDequeue(out splash))
                {
                    break;
                }

                Chunks.World.ParticleManager.Trigger(splash.name, splash.position + new Vector3(0.5f, 0.5f, 0.5f), Color.White, splash.numSplashes);

                if (splashNoiseLimiter[splash.name].HasTriggered)
                {
                    SoundManager.PlaySound(splash.sound, splash.position + new Vector3(0.5f, 0.5f, 0.5f), true);
                }
            }

            foreach (Timer t in splashNoiseLimiter.Values)
            {
                t.Update(time);
            }
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
                // Don't check empty cells or cells we've already modified.
                if (data.Water[i].WaterLevel < 1 || data.Types[i] != 0)
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
            VoxelHandle voxBelow = chunk.MakeVoxel(0, 0, 0);
            VoxelHandle neighbor = new VoxelHandle();

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
                gridCoord = data.CoordsAt(idx);
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
                    voxBelow.GridPosition = new LocalVoxelCoordinate(x, y - 1, z);
                    if (voxBelow.IsEmpty)
                    {
                        cellBelow = voxBelow.Water;
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
                        voxBelow.Water = cellBelow;
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
                                voxBelow.Water = cellBelow;
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
                                voxBelow.Water = cellBelow;
                            }
                        }
                    }
                }

                // Now the only fluid left can spread.
                // We spread to the manhattan neighbors
                //Array.Sort(m_spreadNeighbors, (a, b) => CompareFlowVectors(a, b, data.Water[idx].FluidFlow));
                //m_spreadNeighbors.Shuffle();
                
                for (int s = 0; s < m_spreadNeighbors.Length; s++)
                {
                    var globalPos = chunk.ID + new LocalVoxelCoordinate((int)gridCoord.X, (int)gridCoord.Y, (int)gridCoord.Z) + m_spreadNeighbors[s];

                    // If neighbor is past chunk edge, use GetVoxel, otherwise, use GetVoxelLocal

                    bool success = chunk.Manager.ChunkData.GetVoxel(globalPos, ref neighbor);

                    if (!success || !neighbor.IsEmpty)
                    {
                        continue;
                    }

                    WaterCell neighborWater = neighbor.Water;

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

                    CreateTransfer(globalPos, data.Water[idx], neighborWater, amountToMove);

                    data.Water[idx].WaterLevel -= amountToMove;
                    neighborWater.WaterLevel += amountToMove;

                    if (neighborWater.Type == LiquidType.None)
                    {
                        neighborWater.Type = data.Water[idx].Type;
                    }

                    neighbor.Water = neighborWater;

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
