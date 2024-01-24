using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Collections.Concurrent;
using System.Threading;

namespace DwarfCorp
{
    public class WaterManager
    {
        private ChunkManager Chunks { get; set; }

        public static byte maxWaterLevel = 1;
        public static byte inWaterThreshold = 4;

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
        }

        public void FirstBuild() { 
            foreach (var chunk in Chunks.GetChunkEnumerator())
                EnqueueDirtyChunk(chunk);
        }

        private Queue<LiquidCellHandle> DirtyCells = new Queue<LiquidCellHandle>();

        public void EnqueueDirtyCell(LiquidCellHandle Cell)
        {
            lock (DirtyCells)
            {
                if (!Cell.IsValid || DirtyCells.Contains(Cell)) return;
                DirtyCells.Enqueue(Cell);

                EnqueueDirtyChunk(Cell.Chunk);
            }
        }

        private Queue<VoxelChunk> DirtyChunks = new Queue<VoxelChunk>();

        private void EnqueueDirtyChunk(VoxelChunk Chunk)
        {
            lock (DirtyChunks)
            {
                if (DirtyChunks.Contains(Chunk)) return;
                DirtyChunks.Enqueue(Chunk);
            }
        }

        public void HandleLiquidInteraction(VoxelHandle Vox, byte From, byte To)
        {
            if (From != 0 && To != 0 && From != To)
            {
                if (Library.GetVoxelType("Stone").HasValue(out VoxelType vType))
                    Vox.Type = vType;
                LiquidCellHelpers.ClearVoxelOfLiquid(Vox);
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

        public void UpdateWater()
        {
            if(Chunks.World.Paused || Debugger.Switches.DisableWaterUpdate)
                return;

            ClearDirtyQueue();

            List<VoxelChunk> localDirty = null;
            lock (DirtyChunks)
            {
                localDirty = DirtyChunks.ToList();
                DirtyChunks.Clear();
            }
            foreach (var chunk in localDirty)
                if (chunk != null) chunk.RebuildLiquidGeometry();
        }

        private class OpenSearchNode
        {
            public LiquidCellHandle ParentCell;
            public LiquidCellHandle ThisCell;
            public int Cost;
        }

        private bool TestCell(ChunkManager ChunkManager, LiquidCellHandle SourceCell, LiquidCellHandle Cell)
        {
            if (Cell.LiquidType != 0 && Cell.LiquidType != SourceCell.LiquidType) return false;
            var voxel = ChunkManager.CreateVoxelHandle(Cell.Coordinate.ToGlobalVoxelCoordinate());
            if (voxel.IsValid && !voxel.IsEmpty)
                return false;
            return true;
        }

        private static List<GlobalLiquidOffset> ManhattanNeighbors2D = new List<GlobalLiquidOffset>
        {
            new GlobalLiquidOffset(0,0,-1),
            new GlobalLiquidOffset(1,0,0),
            new GlobalLiquidOffset(0,0,1),
            new GlobalLiquidOffset(-1,0,0)
        };

        private static List<List<GlobalLiquidOffset>> NeighborOrders = EnumeratePermutations(ManhattanNeighbors2D).ToList();

        private static IEnumerable<List<GlobalLiquidOffset>> EnumeratePermutations(List<GlobalLiquidOffset> Of)
        {
            if (Of.Count == 1)
            {
                yield return Of;
                yield break;
            }    

            foreach (var head in Of)
            {
                var r = new List<GlobalLiquidOffset>();
                r.Add(head);
                foreach (var tail in EnumeratePermutations(Of.Where(i => i != head).ToList()))
                    yield return r.Concat(tail).ToList();    
            }
        }

        private IEnumerable<OpenSearchNode> EnumerateOpenNeighbors(ChunkManager ChunkManager, LiquidCellHandle SourceCell, OpenSearchNode Of)
        {
            if (Of.Cost > 500)
                yield break;

            var below = LiquidCellHelpers.GetLiquidCellBelow(Of.ThisCell);
            if (TestCell(ChunkManager, SourceCell, below)) yield return new OpenSearchNode { ParentCell = Of.ThisCell, ThisCell = below, Cost = Of.Cost - 1 };
            if (below.LiquidType == 0) yield break;

            var neighborOrder = MathFunctions.RandInt(0, NeighborOrders.Count);
            foreach (var neighbor in LiquidCellHelpers.EnumerateNeighbors(NeighborOrders[neighborOrder], Of.ThisCell.Coordinate).Select(c => ChunkManager.CreateLiquidCellHandle(c)))
                if (neighbor.IsValid && TestCell(ChunkManager, SourceCell, neighbor)) yield return new OpenSearchNode { ParentCell = Of.ThisCell, ThisCell = neighbor, Cost = Of.Cost + 10 };

            //var above = LiquidCellHelpers.GetLiquidCellAbove(Of.ThisCell);
            //if (TestCell(ChunkManager, SourceCell, above)) yield return new OpenSearchNode { ParentCell = Of.ThisCell, ThisCell = above, Cost = Of.Cost + 100 };
        }

        private LiquidCellHandle FindEmptyCell(ChunkManager ChunkManager, LiquidCellHandle Source)
        {
            var openNodes = new PriorityQueue<OpenSearchNode, int>();
            var closedNodes = new HashSet<GlobalLiquidCoordinate>();
            openNodes.Enqueue(new OpenSearchNode { ParentCell = Source, ThisCell = Source, Cost = 0 }, 0);
            

            while (openNodes.Count > 0)
            {
                var current = openNodes.Dequeue();
                //if (current.ThisCell.Coordinate.Y >= Source.Coordinate.Y)
                //{
                 //   closedNodes.Add(current.ThisCell.Coordinate);
                //    continue;
                //}

                if (current.ThisCell.IsValid && current.ThisCell.LiquidType == 0 && current.ThisCell.Coordinate.Y < Source.Coordinate.Y)
                    return current.ThisCell;

                // Did we find a matching ocean cell? Flow into it!
                if (current.ThisCell.IsValid 
                        && current.ThisCell.Coordinate != Source.Coordinate 
                        && current.ThisCell.LiquidType == Source.LiquidType 
                        && current.ThisCell.OceanFlag == 1)
                    return current.ThisCell;
                
                foreach (var neighbor in EnumerateOpenNeighbors(ChunkManager, Source, current))
                {
                    if (closedNodes.Contains(neighbor.ThisCell.Coordinate)) continue;
                    closedNodes.Add(neighbor.ThisCell.Coordinate);

                    if (neighbor.ThisCell.IsValid)
                        openNodes.Enqueue(neighbor, neighbor.Cost);
                }
            }

            return LiquidCellHandle.InvalidHandle;            
        }

        private void ClearDirtyQueue()
        {
            List<LiquidCellHandle> localDirty = null;
            lock (DirtyCells)
            {
                localDirty = DirtyCells.ToList();
                DirtyCells.Clear();
            }
            foreach (var cell in localDirty)
                UpdateCell(Chunks, cell);
        }

        private void UpdateCell(ChunkManager ChunkManager, LiquidCellHandle dirtyCell)
        {
            if (!dirtyCell.IsValid)
                return;

            if (dirtyCell.LiquidType == 0)
                return;

            if (dirtyCell.OceanFlag == 1)
                return;

            var above = LiquidCellHelpers.GetLiquidCellAbove(dirtyCell);
            if (above.IsValid && above.LiquidType != 0 && above.LiquidType == dirtyCell.LiquidType) return;

            var emptyNeighborCount = 0;
            foreach (var neighbor in LiquidCellHelpers.EnumerateManhattanNeighbors2D_Y(dirtyCell.Coordinate).Select(c => ChunkManager.CreateLiquidCellHandle(c)))
                if (neighbor.IsValid && neighbor.LiquidType == 0)
                    emptyNeighborCount += 1;

            if (emptyNeighborCount == 0)
                return;

            var destinationCell = FindEmptyCell(ChunkManager, dirtyCell);
            if (!destinationCell.IsValid)
                return;

            CreateSplash(dirtyCell.Center * 0.5f, dirtyCell.LiquidType);

            destinationCell.LiquidType = dirtyCell.LiquidType;
            if (dirtyCell.OceanFlag == 0)
                dirtyCell.LiquidType = 0;            

            EnqueueDirtyCell(LiquidCellHelpers.GetLiquidCellAbove(dirtyCell));
            EnqueueDirtyCell(LiquidCellHelpers.GetLiquidCellBelow(dirtyCell));
            foreach (var neighbor in LiquidCellHelpers.EnumerateManhattanNeighbors2D_Y(dirtyCell.Coordinate).Select(c => ChunkManager.CreateLiquidCellHandle(c)))
                EnqueueDirtyCell(neighbor);

            if (destinationCell.OceanFlag != 1)
                EnqueueDirtyCell(destinationCell);
        }
    }
}
