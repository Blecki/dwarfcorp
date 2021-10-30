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

        public static byte maxWaterLevel = 63;
        public static byte rainFallAmount = 1;
        public static byte inWaterThreshold = 32;

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

        public void HandleLiquidInteraction(VoxelHandle Vox, byte From, byte To)
        {
            if (From != 0 && To != 0 && From != To)
            {
                if (Library.GetVoxelType("Stone").HasValue(out VoxelType vType))
                    Vox.Type = vType;
                Vox.QuickSetLiquid(0, 0);
            }            
        }

        public void CreateSplash(Vector3 pos, int liquid)
        {
            if (MathFunctions.RandEvent(0.25f)) return;

            if (Library.GetLiquid(liquid).HasValue(out var liquidType))
            {
                var splash = new LiquidSplash
                {
                    name = liquidType.SplashName,
                    numSplashes = liquidType.SplashCount,
                    position = pos,
                    sound = liquidType.SplashSound,
                    entity = liquidType.SplashEntity
                };

                SplashLock.WaitOne();
                Splashes.AddFirst(splash);
                SplashLock.ReleaseMutex();
            }
        }

        public float GetSpreadRate(int type)
        {
            if (Library.GetLiquid(type).HasValue(out var liquidType))
                return liquidType.SpreadRate;
            return 1.0f;
        }

        public void UpdateWater()
        {
            if(Chunks.World.Paused || Debugger.Switches.DisableWaterUpdate)
                return;
            
            foreach(var chunk in Chunks.GetChunkEnumerator())
            {
                DiscreteUpdate(Chunks, chunk);
                chunk.RebuildLiquidGeometry();
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

                    if (currentVoxel.LiquidType == 0 || currentVoxel.LiquidLevel < 1)
                        continue;

                    LiquidType_ currentVoxelLiquidType;
                    if (!Library.GetLiquid(currentVoxel.LiquidType).HasValue(out currentVoxelLiquidType))
                        continue;

                    // Evaporate.
                    if (currentVoxel.LiquidLevel <= EvaporationLevel && MathFunctions.RandEvent(0.01f))
                    {
                        if (currentVoxelLiquidType.EvaporateToStone && Library.GetVoxelType("Stone").HasValue(out VoxelType stone))
                            currentVoxel.Type = stone;

                        NeedsMinimapUpdate = true;
                        currentVoxel.QuickSetLiquid(0, 0);
                        continue;
                    }

                    var voxBelow = ChunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(currentVoxel.Coordinate.X, currentVoxel.Coordinate.Y - 1, currentVoxel.Coordinate.Z));

                    if (voxBelow.IsValid && voxBelow.IsEmpty)
                    {
                        // Fall into the voxel below.

                        // Special case: No liquid below, just drop down.
                        if (voxBelow.LiquidType == 0)
                        {
                            NeedsMinimapUpdate = true;
                            CreateSplash(currentVoxel.Coordinate.ToVector3(), currentVoxel.LiquidType);
                            voxBelow.QuickSetLiquid(currentVoxel.LiquidType, currentVoxel.LiquidLevel);
                            currentVoxel.QuickSetLiquid(0, 0);
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
                            currentVoxel.QuickSetLiquid(0, 0);
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
                    else if (voxBelow.IsValid && currentVoxelLiquidType.ClearsGrass && !voxBelow.IsEmpty && voxBelow.GrassType > 0)
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
                                    CreateSplash(neighborVoxel.Coordinate.ToVector3(), currentVoxel.LiquidType);

                                var newWater = currentVoxel.LiquidLevel - amountToMove;

                                var sourceType = currentVoxel.LiquidType;
                                var destType = neighborVoxel.LiquidType;
                                currentVoxel.QuickSetLiquid(newWater == 0 ? (byte)0 : sourceType, (byte)newWater);
                                neighborVoxel.QuickSetLiquid(destType == 0 ? sourceType : destType, (byte)(neighborVoxel.LiquidLevel + amountToMove));
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
