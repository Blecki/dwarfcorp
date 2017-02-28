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
using System.Diagnostics;

namespace DwarfCorp
{
    public enum LiquidType
    {
        None = 0,
        Water,
        Lava,
        Count
    }

    public class LiquidFlowRules
    {
        public float inLiquidThreshold;
        public float rebuildThreshold;
        public float movementThreshold;
        public float evaporationChance;
        public float evaporationLevel;
    }

    public class UnsettledLiquid
    {
        Voxel voxel;
        bool evaporateCheck;
        bool flowCheck;

        public UnsettledLiquid(Voxel voxel)
        {
            Set(voxel, false, true);
        }

        public UnsettledLiquid(Voxel voxel, bool evaporate, bool flowCheck)
        {
            Set(voxel, evaporate, flowCheck);
        }

        // Constructor routes through here to avoid duplicating code.
        public void Set(Voxel voxel, bool evaporateCheck, bool flowCheck)
        {
            if (this.voxel == null)
                this.voxel = new Voxel(voxel);
            else
                this.voxel.CopyFrom(voxel);

            this.evaporateCheck = evaporateCheck;
            this.flowCheck = flowCheck;
        }

        public bool FlowCheck
        {
            get { return flowCheck; }
            set { flowCheck = value; }
        }

        public bool EvaporateCheck
        {
            get { return evaporateCheck; }
            set { evaporateCheck = value; }
        }

        public Voxel Voxel
        {
            get { return voxel; }
        }
    }

    /// <summary>
    /// Handles the water simulation in the game.
    /// </summary>
    public class WaterManager
    {
        private Dictionary<string, Timer> splashNoiseLimiter = new Dictionary<string, Timer>();
        private ChunkManager Chunks { get; set; }
        public byte EvaporationLevel { get; set; }

        public static Dictionary<LiquidType, LiquidFlowRules> flowRules;

        public static byte maxWaterLevel = 127;
        public static byte threeQuarterWaterLevel;
        public static byte oneHalfWaterLevel;
        public static byte oneQuarterWaterLevel;
        public static byte inWaterThreshold;
        public static byte waterMoveThreshold = 1;
        public static byte rebuildThreshold = 1;
        public static float evaporationChance = 0.01f;
        public static byte rainFallAmount;

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
        private ConcurrentDictionary<Vector3, UnsettledLiquid> unsettledLiquids;


        public static Vector3[] m_spreadNeighbors =
        {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1)
        };

        // Here just so I can call functions in WaterManager in GamePerformance.
        public static WaterManager instance;

        public WaterManager(ChunkManager chunks)
        {
            instance = this;
            InitializeStatics();
            Chunks = chunks;
            EvaporationLevel = (byte)(oneQuarterWaterLevel / 2f);
            Splashes = new ConcurrentQueue<SplashType>();
            Transfers = new ConcurrentQueue<Transfer>();
            updatedChunks = new Dictionary<VoxelChunk, bool>();
            unsettledLiquids = new ConcurrentDictionary<Vector3, UnsettledLiquid>();

            splashNoiseLimiter["splash2"] = new Timer(0.1f, false);
            splashNoiseLimiter["flame"] = new Timer(0.1f, false);

            flowRules = new Dictionary<LiquidType, LiquidFlowRules>();
            flowRules.Add(LiquidType.Water, new LiquidFlowRules()
            {
                inLiquidThreshold = 5 / 8f,
                rebuildThreshold = 1/8f,
                movementThreshold = 1,
                evaporationChance = 0.01f,
                evaporationLevel = 1/9f                  
            });
        }

        public void InitializeStatics()
        {
            float max = maxWaterLevel;
            float oneEighth = max / 8;

            threeQuarterWaterLevel = (byte)(Math.Round(oneEighth * 6));
            oneHalfWaterLevel = (byte)(Math.Round(oneEighth * 4));
            oneQuarterWaterLevel = (byte)(Math.Round(oneEighth * 2));
            rainFallAmount = (byte)(Math.Round(oneEighth));
            inWaterThreshold = (byte)(Math.Round(oneEighth * 5));
            waterMoveThreshold = 1;
        }

        public static int waterTransfers = 0;
        public void CreateTransfer(Vector3 worldPosition, WaterCell water1, WaterCell water2, byte amount)
        {
            waterTransfers++;
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
            switch (liquid)
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
            var screenRect = new Rectangle(0, 0, 5, 5);
            Vector3 half = Vector3.One * 0.5f;

            Queue<Transfer> transferBackup = new Queue<Transfer>(Transfers.Count);
            while (Transfers.Count > 0)
            {
                Transfer transfer;

                if (!Transfers.TryDequeue(out transfer))
                {
                    break;
                }
                transferBackup.Enqueue(transfer);

                if (GamePerformance.DebugToggle2)
                {
                    Color drawColor = Color.White;
                    if (transfer.amount > threeQuarterWaterLevel) drawColor = Color.Blue;
                    else if (transfer.amount > oneHalfWaterLevel) drawColor = Color.Green;
                    else if (transfer.amount > oneQuarterWaterLevel) drawColor = Color.Yellow;
                    else if (transfer.amount > waterMoveThreshold) drawColor = Color.Orange;
                    else drawColor = Color.Red;
                    Drawer2D.DrawRect(Chunks.World.Camera, transfer.worldLocation + half, screenRect, drawColor, Color.Transparent, 0.0f);
                }

                if ((transfer.cellFrom.Type == LiquidType.Lava
                && transfer.cellTo.Type == LiquidType.Water) ||
                (transfer.cellFrom.Type == LiquidType.Water && transfer.cellTo.Type == LiquidType.Lava))
                {
                    bool success = Chunks.ChunkData.GetVoxel(transfer.worldLocation, ref atPos);

                    if (success)
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

            //while (transferBackup.Count > 0)
            //{
            //    Transfers.Enqueue(transferBackup.Dequeue());
            //}
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

        public static int waterTotal;

        public void OnVoxelDestroyed(Voxel v)
        {
            Vector3 successor = Vector3.Up;
            Voxel neighbor = new Voxel();
            // This is intentionally going one over the array length so we can sneak in a Vector3.Up as well.
            for (int i = 0; i <= m_spreadNeighbors.Length; i++)
            {
                if (i < m_spreadNeighbors.Length)
                {
                    successor = m_spreadNeighbors[i];
                }
                else
                {
                    successor = Vector3.Up;
                }

                if (!v.GetNeighborBySuccessor(successor, ref neighbor, false)) continue;
                if (neighbor.WaterLevel > 0)
                {
                    AddUnsettledWater(neighbor);
                }
            }
        }

        /// <summary>
        /// Adds a new UnsettledLiquid object to the dictionary.  Does nothing if the key already exists.
        /// </summary>
        /// <param name="v">The voxel pointing to the unsettled water.  Voxel object will be copied and the copy used instead.</param>
        public void AddUnsettledWater(Voxel v)
        {
            if (v == null) return;
            Vector3 pos = v.Position;
            if (unsettledLiquids.ContainsKey(pos)) return;
            bool added = unsettledLiquids.TryAdd(pos, new UnsettledLiquid(v, false, true));
            Debug.Assert(added, "Failure adding unsettled liquid to the list");
        }

        /// <summary>
        /// Removes an UnsettledLiquid object from the list.  Does nothing if the key doesn't exist.
        /// </summary>
        /// <param name="v">The voxel pointing to the unsettled water.</param>
        public void RemoveUnsettledWater(Voxel v)
        {
            if (v == null) return;
            if (!unsettledLiquids.ContainsKey(v.Position)) return;
            UnsettledLiquid ignored;
            bool removed = unsettledLiquids.TryRemove(v.Position, out ignored);
            Debug.Assert(removed, "Failure removing unsettled liquid from the list");
        }

        private void RemoveUnsettledWaterIfNeeded(UnsettledLiquid ul)
        {
            if (ul == null) return;
            if (ul.EvaporateCheck || ul.FlowCheck) return;
            RemoveUnsettledWater(ul.Voxel);
        }

        public void FindAllUnsettledWater()
        {
            List<VoxelChunk> chunksToCheck = Chunks.ChunkData.ChunkMap.Select(chunks => chunks.Value).ToList();

            foreach (VoxelChunk chunk in chunksToCheck)
            {
                foreach (Voxel v in chunk.GetAllVoxelsWithWater())
                {
                    if (!IsLiquidStable(v))
                    {
                        CreateTransfer(v.Position, new WaterCell(), new WaterCell(), 255);
                        AddUnsettledWater(v);
                    }
                }
                GamePerformance.Instance.TrackValueType("unsettledLiquid.Count", unsettledLiquids.Count);
            }
        }

        public void ConvertAllWater(byte oldMaximum)
        {
            List<VoxelChunk> chunksToCheck = Chunks.ChunkData.ChunkMap.Select(chunks => chunks.Value).ToList();

            foreach (VoxelChunk chunk in chunksToCheck)
            {
                foreach (Voxel v in chunk.GetAllVoxelsWithWater())
                {
                    float waterLevel = v.WaterLevel;

                    float percent = waterLevel / oldMaximum;
                    float newWater = (float)Math.Round(maxWaterLevel * percent);

                    v.WaterLevel = (byte)(newWater);
                }
            }
        }

        public bool IsLiquidStable(Voxel v)
        {
            if (v == null) return true;

            // No water is a stable condition.
            // We will count on other nearby water sources being marked unstable to fill if need be.
            WaterCell mainWaterCell = v.Water;
            if (mainWaterCell.WaterLevel == 0) return true;
            if (mainWaterCell.WaterLevel <= EvaporationLevel) return false;

            LiquidType mainWaterType = mainWaterCell.Type;
            VoxelChunk chunk = v.Chunk;
            WaterCell comparisonWaterCell;
            Voxel neighbor = chunk.MakeVoxel(0, 0, 0);
            if (v.GetNeighborBySuccessor(Vector3.Down, ref neighbor, false))
            {
                if (neighbor.IsEmpty)
                {
                    comparisonWaterCell = neighbor.Water;
                    if (comparisonWaterCell.WaterLevel < maxWaterLevel || mainWaterType != comparisonWaterCell.Type) return false;
                }
            }
            for (int i = 0; i < m_spreadNeighbors.Length; i++)
            {
                if (!v.GetNeighborBySuccessor(m_spreadNeighbors[i], ref neighbor, false)) continue;
                if (!neighbor.IsEmpty) continue;
                comparisonWaterCell = neighbor.Water;
                if (Math.Abs(mainWaterCell.WaterLevel - comparisonWaterCell.WaterLevel) > waterMoveThreshold || mainWaterType != comparisonWaterCell.Type) return false;
            }
            return true;
        }

        public static int ticksUntilSettled = 0;
        public static int ticksToRun = 0;
        public Dictionary<VoxelChunk, bool> updatedChunks;

        public void UpdateWater()
        {
            if (Chunks.World.Paused)
            {
                return;
            }

            if (ticksToRun == 0) return;

            Transfer transfer;
            while (Transfers.Count > 0)
                Transfers.TryDequeue(out transfer);

            ticksUntilSettled++;

            GamePerformance.Instance.StartTrackPerformance("WaterManager.UpdateWater");
            updatedChunks.Clear();

            waterTotal = 0;
            waterTransfers = 0;
            UpdateUnsettledWater(updatedChunks);
            foreach (KeyValuePair<VoxelChunk, bool> kvp in updatedChunks)
            {
                if (kvp.Value != true) Debugger.Break();

                kvp.Key.ShouldRebuildWater = true;
            }
            GamePerformance.Instance.StopTrackPerformance("WaterManager.UpdateWater");
            ticksToRun--;
            GamePerformance.Instance.TrackValueType("ticksUntilSettled", ticksUntilSettled);
            GamePerformance.Instance.TrackValueType("waterTotal", waterTotal);
            GamePerformance.Instance.TrackValueType("waterTransfers", waterTransfers);

            if (unsettledLiquids.Count == 0) ticksToRun = 0;
            else ticksToRun = 1;
        }

        public void UpdateUnsettledWater(Dictionary<VoxelChunk, bool> chunksToUpdate)
        {
            if (unsettledLiquids.Count == 0) return;

            // Temporary while I do testing.
            while (Transfers.Count > 0)
            {
                Transfer r;
                Transfers.TryDequeue(out r);
            }

            List<Voxel> neighbors = new List<Voxel>();
            for (int i = 0; i < 4; i++)
            {
                neighbors.Add(new Voxel());
            }

            ChunkData chunkData = Chunks.World.ChunkManager.ChunkData;

            Voxel centerVoxel = new Voxel();
            Voxel neighbor = new Voxel();
            Voxel underNeighbor = new Voxel();

            Voxel[] backupNeighbors = new Voxel[4];

            Voxel[] waterSpreadOrder = new Voxel[5];
            byte[] waterSpreadAmount = new byte[5];

            GamePerformance.Instance.TrackValueType("unsettledLiquids.Count", unsettledLiquids.Count);
            foreach(KeyValuePair<Vector3, UnsettledLiquid> uLiquid in unsettledLiquids)
            { 
                neighbor = neighbors[0];

                centerVoxel = uLiquid.Value.Voxel;
                VoxelChunk chunk = centerVoxel.Chunk;
                Vector3 worldPos = centerVoxel.Position;

                WaterCell centerWaterCell = centerVoxel.Water;

                if (centerWaterCell.WaterLevel > 0 && centerWaterCell.WaterLevel <= EvaporationLevel)
                {
                    uLiquid.Value.EvaporateCheck = true;
                    if (MathFunctions.RandEvent(evaporationChance))
                    {
                        if (centerWaterCell.WaterLevel > 1)
                            centerWaterCell.WaterLevel--;
                        else
                        {
                            centerWaterCell.WaterLevel = 0;

                            if (centerWaterCell.Type == LiquidType.Lava)
                            {
                                // TODO: This is a hack to avoid creating the tile right here and now using the current transfer system.  Fix it
                                CreateTransfer(worldPos, new WaterCell() { Type = LiquidType.Lava }, new WaterCell() { Type = LiquidType.Water }, 1);
                            }

                            centerWaterCell.Type = LiquidType.None;
                        }
                        // Assign the value back as it's changed.
                        centerVoxel.Water = centerWaterCell;
                        chunksToUpdate[chunk] = true;
                    }
                }
                else if (uLiquid.Value.EvaporateCheck)
                    uLiquid.Value.EvaporateCheck = false;

                // If we've evaporated the water we're done with this voxel.
                if (centerWaterCell.WaterLevel == 0)
                {
                    uLiquid.Value.FlowCheck = false;
                    RemoveUnsettledWaterIfNeeded(uLiquid.Value);
                    continue;
                }

                // Check the cell below the current one.
                if (centerVoxel.GetNeighborBySuccessor(Vector3.Down, ref neighbor, false))
                {
                    // Only bother to try if there is room in the tile.
                    if (neighbor.IsEmpty && neighbor.WaterLevel < maxWaterLevel)
                    {
                        WaterCell cellBelow = neighbor.Water;

                        // If the cell immediately below us is empty,
                        // swap the contents and move on.
                        if (cellBelow.WaterLevel == 0)
                        {
                            CreateSplash(worldPos, centerWaterCell.Type);
                            // Copy the cell over direct and empty the old one.
                            cellBelow = centerWaterCell;
                            centerWaterCell.Clear();

                            // Do a mostly pointless water transfer creation.  Useful for now thanks to the visualization.
                            CreateTransfer(worldPos, centerWaterCell, cellBelow, cellBelow.WaterLevel);
                            AddUnsettledWater(neighbor);


                            // Reassign the structs.
                            neighbor.Water = cellBelow;
                            centerVoxel.Water = centerWaterCell;

                            // Set the chunks to update.
                            chunksToUpdate[chunk] = true;
                            if (neighbor.Chunk != chunk)
                                chunksToUpdate[chunk] = true;

                            // To the next tile as we've emptied this one.
                            RemoveUnsettledWaterIfNeeded(uLiquid.Value);
                            continue;
                        }
                        else
                        {
                            byte spaceLeft = (byte)(maxWaterLevel - cellBelow.WaterLevel);

                            // Special case where we can flow completely into the next cell.
                            if (spaceLeft >= centerWaterCell.WaterLevel)
                            {
                                byte transfer = centerWaterCell.WaterLevel;
                                cellBelow.WaterLevel += transfer;
                                if (cellBelow.Type == LiquidType.None)
                                    cellBelow.Type = centerWaterCell.Type;
                                centerWaterCell.Clear();

                                // Do a water transfer creation.  Checks for water/lava collisions.
                                CreateTransfer(worldPos, centerWaterCell, cellBelow, transfer);
                                AddUnsettledWater(neighbor);

                                // Reassign the structs.
                                neighbor.Water = cellBelow;
                                centerVoxel.Water = centerWaterCell;

                                // Set the chunks to update.
                                chunksToUpdate[chunk] = true;
                                if (neighbor.Chunk != chunk)
                                    chunksToUpdate[chunk] = true;

                                // To the next tile as we've emptied this one.
                                RemoveUnsettledWaterIfNeeded(uLiquid.Value);
                                continue;
                            }
                            else
                            {
                                centerWaterCell.WaterLevel -= spaceLeft;
                                cellBelow.WaterLevel += spaceLeft;
                                if (cellBelow.Type == LiquidType.None)
                                    cellBelow.Type = centerWaterCell.Type;

                                // Do a water transfer creation.  Checks for water/lava collisions.
                                CreateTransfer(worldPos, centerWaterCell, cellBelow, spaceLeft);
                                AddUnsettledWater(neighbor);

                                // Reassign the structs.
                                neighbor.Water = cellBelow;
                                centerVoxel.Water = centerWaterCell;


                                // TODO: Find a better way to handle updates here.
                                // We could be dropping from 8 water level down to 1 water level
                                // and this code will not force a rebuild.
                                // Set the chunks to update.
                                //chunksToUpdate[chunk] = true;
                                //if (neighbor.Chunk != chunk)
                                //    chunksToUpdate[chunk] = true;
                            }
                        }
                    }
                }

                // Now the only fluid left can spread.
                // We spread to the manhattan neighbors
                m_spreadNeighbors.Shuffle();

                int validWaterCellCount = 0;

                for (int i = 0; i < waterSpreadAmount.Length; i++)
                {
                    waterSpreadOrder[i] = null;
                    waterSpreadAmount[i] = 0;
                }

                waterSpreadOrder[validWaterCellCount] = centerVoxel;
                waterSpreadAmount[validWaterCellCount] = centerWaterCell.WaterLevel;
                validWaterCellCount++;

                for (int i = 0; i < m_spreadNeighbors.Length; i++)
                {
                    backupNeighbors[i] = new Voxel(neighbors[i]);
                    neighbor = neighbors[i];
                    if (!centerVoxel.GetNeighborBySuccessor(m_spreadNeighbors[i], ref neighbor, false)) continue;
                    if (!neighbor.IsEmpty) continue;

                    waterSpreadOrder[validWaterCellCount] = neighbor;
                    waterSpreadAmount[validWaterCellCount] = neighbor.WaterLevel;
                    validWaterCellCount++;
                }

                // We have no neighbors to spread to.  Consider this case closed.
                if (validWaterCellCount == 1)
                {
                    uLiquid.Value.FlowCheck = false;
                    RemoveUnsettledWaterIfNeeded(uLiquid.Value);
                    continue;
                }

                // We're going to sort the cells now.  This isn't efficient but it's only five tiles so hopefully it's enough.
                int cellSwapCount;
                do
                {
                    cellSwapCount = 0;
                    for (int i = 0; i < validWaterCellCount - 1; i++)
                    {
                        if (waterSpreadAmount[i] > waterSpreadAmount[i + 1])
                        {
                            byte waterSwap = waterSpreadAmount[i];
                            waterSpreadAmount[i] = waterSpreadAmount[i + 1];
                            waterSpreadAmount[i + 1] = waterSwap;

                            Voxel voxelSwap = waterSpreadOrder[i];
                            waterSpreadOrder[i] = waterSpreadOrder[i + 1];
                            waterSpreadOrder[i + 1] = voxelSwap;

                            cellSwapCount++;
                        }
                    }
                } while (cellSwapCount > 0);

                int waterLevel = 0;
                for (int i = 0; i < validWaterCellCount; i++)
                {
                    waterLevel += waterSpreadAmount[i];
                }

                byte waterCellAve = (byte)(waterLevel / validWaterCellCount);
                byte waterCellRemainder = (byte)(waterLevel % validWaterCellCount);

                int perfectMatches = 0;
                int oneUpMatches = 0;
                bool unsettled = false;
                for (int i = 0; i < validWaterCellCount; i++)
                {
                    byte water = waterSpreadAmount[i];
                    if (water == waterCellAve) perfectMatches++;
                    else if (water == waterCellAve + 1) oneUpMatches++;
                    else { unsettled = true; break; }
                }
                if (!unsettled)
                {
                    if (perfectMatches == (validWaterCellCount - waterCellRemainder) &&
                        oneUpMatches == waterCellRemainder)
                    {
                        uLiquid.Value.FlowCheck = false;
                        RemoveUnsettledWaterIfNeeded(uLiquid.Value);
                        continue;
                    }
                }

                for (int i = 0; i < validWaterCellCount; i++)
                {
                    byte water = waterSpreadAmount[i];
                    byte waterToSet = 0;
                    if (waterCellRemainder > 0)
                    {
                        waterToSet = (byte)(waterCellAve + 1);
                        waterCellRemainder--;
                    }
                    else
                    {
                        waterToSet = waterCellAve;
                    }

                    byte difference;
                    if (water > waterToSet) difference = (byte)(water - waterToSet);
                    else difference = (byte)(waterToSet - water);

                    if (difference == 0) continue;


                    neighbor = waterSpreadOrder[i];

                    if (difference >= rebuildThreshold)
                    {
                        // neighbor == centerVoxel is a perfectly valid equals as we are comparing reference addresses.
                        // centerVoxel is put into the neighbor list as a reference.
                        if (neighbor == centerVoxel || neighbor.Chunk != chunk)
                            chunksToUpdate[neighbor.Chunk] = true;
                    }

                    neighbor.Water = new WaterCell(waterToSet, centerWaterCell.Type);
                    AddUnsettledWater(neighbor);
                    CreateTransfer(neighbor.Position, neighbor.Water, centerWaterCell, difference);
                }
            }
        }
    }
}
