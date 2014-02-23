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
            EvaporationLevel = 5;
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
            switch(liquid)
            {
                case LiquidType.Water:
                {
                    SplashType splash = new SplashType
                    {
                        name = "splash2",
                        numSplashes = 2,
                        position = pos,
                        sound = Program.CreatePath("Audio", "river")
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
                        sound = Program.CreatePath("Audio", "river")
                    };
                    Splashes.Enqueue(splash);
                }
                    break;
            }
        }

        public void HandleTransfers(GameTime time)
        {
            while(Transfers.Count > 0)
            {
                Transfer transfer;

                if(!Transfers.TryDequeue(out transfer))
                {
                    break;
                }

                if(transfer.cellFrom.Type == LiquidType.Lava && transfer.cellTo.Type == LiquidType.Water || (transfer.cellFrom.Type == LiquidType.Water && transfer.cellTo.Type == LiquidType.Lava))
                {
                    VoxelRef atPos = Chunks.ChunkData.GetVoxelReferenceAtWorldLocation(transfer.worldLocation);

                    if(atPos != null)
                    {
                        VoxelRef v = atPos;

                        VoxelChunk chunk = Chunks.ChunkData.ChunkMap[v.ChunkID];
                        chunk.VoxelGrid[(int) v.GridPosition.X][(int) v.GridPosition.Y][(int) v.GridPosition.Z] = new Voxel(v.WorldPosition, VoxelLibrary.GetVoxelType("Stone"), VoxelLibrary.GetPrimitive("Stone"), true);
                        chunk.VoxelGrid[(int) v.GridPosition.X][(int) v.GridPosition.Y][(int) v.GridPosition.Z].Chunk = Chunks.ChunkData.ChunkMap[v.ChunkID];
                        chunk.Water[(int) v.GridPosition.X][(int) v.GridPosition.Y][(int) v.GridPosition.Z].Type = LiquidType.None;
                        chunk.Water[(int) v.GridPosition.X][(int) v.GridPosition.Y][(int) v.GridPosition.Z].WaterLevel = 0;
                        chunk.ShouldRebuild = true;
                        chunk.ShouldRecalculateLighting = true;
                    }
                }
            }
        }

        public void Splash(GameTime time)
        {
            while(Splashes.Count > 0)
            {
                SplashType splash;

                if(!Splashes.TryDequeue(out splash))
                {
                    break;
                }

                PlayState.ParticleManager.Trigger(splash.name, splash.position + new Vector3(0.5f, 0.5f, 0.5f), Color.White, splash.numSplashes);

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
                    return 0.1f + (float) PlayState.Random.NextDouble() * 0.1f;
                case LiquidType.Water:
                    return 0.5f;
            }


            return 1.0f;
        }

        public void UpdateWater()
        {
            if(PlayState.Paused)
            {
                return;
            }

            List<VoxelChunk> chunksToUpdate = Chunks.ChunkData.ChunkMap.Select(chunks => chunks.Value).ToList();

            chunksToUpdate.Sort(Chunks.CompareChunkDistance);

            foreach(VoxelChunk chunk in chunksToUpdate)
            {
                if(chunk.ShouldRebuildWater || chunk.FirstWaterIter)
                {
                    chunk.ResetWaterBuffer();
                }

                if(!UpdateChunk(chunk) && !chunk.FirstWaterIter)
                {
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

            List<Point3> updateList = new List<Point3>();


            for(int x = 0; x < chunk.SizeX; x++)
            {
                for(int y = 0; y < chunk.SizeY; y++)
                {
                    for(int z = 0; z < chunk.SizeZ; z++)
                    {
                        WaterCell cell = chunk.Water[x][y][z];

                        // Don't check empty cells or cells we've already modified.
                        if(cell.WaterLevel < 1 || chunk.VoxelGrid[x][y][z] != null)
                        {
                            continue;
                        }
                        updateList.Add(new Point3(x, y, z));
                    }
                }
            }

            if(updateList.Count == 0)
            {
                return false;
            }


            List<int> indices = Datastructures.RandomIndices(updateList.Count);

            // Loop through each cell.
            foreach(int t in indices)
            {
                Point3 point = updateList[t];
                int x = point.X;
                int y = point.Y;
                int z = point.Z;

                WaterCell cell = chunk.Water[x][y][z];

                // Don't check empty cells or cells we've already modified.
                if(cell.WaterLevel < 1 || chunk.VoxelGrid[x][y][z] != null)
                {
                    continue;
                }

                gridCoord.X = x;
                gridCoord.Y = y;
                gridCoord.Z = z;
                Vector3 worldPos = gridCoord + chunk.Origin;

                if(cell.WaterLevel < EvaporationLevel && PlayState.Random.Next(0, 10) < 5)
                {
                    if(cell.WaterLevel > 1)
                    {
                        cell.WaterLevel--;
                    }
                    else
                    {
                        cell.WaterLevel = 0;
                        cell.Type = LiquidType.None;
                        CreateSplash(worldPos, cell.Type);
                    }
                    updateOccurred = true;
                }


                bool shouldFall = false;

                WaterCell cellBelow = null;
                // Now check the cell immediately below this one.
                // There are two cases, either we are at the bottom of the chunk,
                // in which case we must find the water from the chunk manager.
                // Otherwise, we just get the cell immediately beneath us.
                if(y > 0)
                {
                    if(chunk.VoxelGrid[x][y - 1][z] == null)
                    {
                        cellBelow = chunk.Water[x][y - 1][z];
                        shouldFall = true;
                    }
                }
                else
                {
                    if(chunk.Manager.ChunkData.DoesWaterCellExist(worldPos))
                    {
                        VoxelRef voxelsBelow = chunk.Manager.ChunkData.GetVoxelReferenceAtWorldLocation(chunk, worldPos + new Vector3(0, -1, 0));

                        if(voxelsBelow != null && voxelsBelow.TypeName == "empty")
                        {
                            cellBelow = chunk.Manager.ChunkData.GetWaterCellAtLocation(worldPos + new Vector3(0, -1, 0));
                            shouldFall = true;
                            cellBelow.IsFalling = true;
                        }
                    }
                }

                // Cases where the fluid can fall down.
                if(shouldFall)
                {
                    // If the cell immediately below us is empty,
                    // swap the contents and move on.
                    if(cellBelow.WaterLevel < 1)
                    {
                        cellBelow.WaterLevel = cell.WaterLevel;
                        if(cellBelow.Type == LiquidType.None)
                        {
                            cellBelow.Type = cell.Type;
                        }
                        cell.WaterLevel = 0;
                        cell.Type = LiquidType.None;
                        cell.IsFalling = true;
                        cell.HasChanged = true;
                        cellBelow.HasChanged = true;
                        CreateSplash(worldPos, cell.Type);
                        CreateTransfer(worldPos, cell, cellBelow, cellBelow.WaterLevel);
                        updateOccurred = true;
                        continue;
                    }
                        // Otherwise, fill as much of the space as we can.
                    else
                    {
                        byte spaceLeft = (byte) (255 - cellBelow.WaterLevel);

                        if(spaceLeft > 5)
                        {
                            CreateSplash(gridCoord + chunk.Origin, cell.Type);
                        }

                        // Special case where we can flow completely into the next cell.
                        if(spaceLeft >= cell.WaterLevel)
                        {
                            byte transfer = cell.WaterLevel;
                            cellBelow.WaterLevel += transfer;
                            cellBelow.HasChanged = true;
                            if(cellBelow.Type == LiquidType.None)
                            {
                                cellBelow.Type = cell.Type;
                            }
                            cell.WaterLevel = 0;
                            cell.Type = LiquidType.None;
                            cell.HasChanged = true;
                            CreateTransfer(worldPos - Vector3.UnitY, cell, cellBelow, transfer);
                            updateOccurred = true;
                            continue;
                        }
                            // Otherwise, only flow a little bit, and spread later.
                        else
                        {
                            cell.WaterLevel -= spaceLeft;
                            cellBelow.WaterLevel += spaceLeft;
                            cell.HasChanged = true;
                            cellBelow.HasChanged = true;
                            if(cellBelow.Type == LiquidType.None)
                            {
                                cellBelow.Type = cell.Type;
                            }
                            CreateTransfer(worldPos - Vector3.UnitY, cell, cellBelow, spaceLeft);
                        }
                    }
                }

                // Now the only fluid left can spread.
                // We spread to the manhattan neighbors
                Array.Sort(m_spreadNeighbors, (a, b) => CompareFlowVectors(a, b, cell.FluidFlow));

                foreach(Vector3 spread in m_spreadNeighbors)
                {
                    VoxelRef neighbor = chunk.Manager.ChunkData.GetVoxelReferenceAtWorldLocation(chunk, worldPos + spread);

                    if(neighbor == null)
                    {
                        continue;
                    }

                    if(neighbor.TypeName != "empty")
                    {
                        continue;
                    }

                    WaterCell neighborWater = neighbor.GetWater(Chunks);

                    if(neighborWater == null)
                    {
                        continue;
                    }

                    byte amountToMove = (byte) (Math.Min(255.0f - (float) neighborWater.WaterLevel, cell.WaterLevel) * GetSpreadRate(cell.Type));

                    if(amountToMove == 0)
                    {
                        continue;
                    }


                    if(neighborWater.WaterLevel < 2)
                    {
                        CreateSplash(worldPos + spread, cell.Type);
                        updateOccurred = true;
                    }

                    CreateTransfer(worldPos + spread, cell, neighborWater, amountToMove);

                    cell.WaterLevel -= amountToMove;
                    neighborWater.WaterLevel += amountToMove;

                    if(neighborWater.Type == LiquidType.None)
                    {
                        neighborWater.Type = cell.Type;
                    }

                    cell.FluidFlow = spread + MathFunctions.RandVector3Cube() * 0.5f;
                    neighborWater.FluidFlow = spread + MathFunctions.RandVector3Cube() * 0.5f;
                    cell.HasChanged = true;
                    neighborWater.HasChanged = true;


                    if(cell.WaterLevel >= 1)
                    {
                        continue;
                    }

                    cell.WaterLevel = 0;
                    cell.Type = LiquidType.None;
                    break;
                }

                cell.FluidFlow = Vector3.Zero;
            }

            return updateOccurred;
        }

        public bool UpdateChunk(VoxelChunk chunk)
        {
            return DiscreteUpdate(chunk);
        }
    }

}