using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DwarfCorpCore;
using Microsoft.Xna.Framework;
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

        public ChunkData(uint chunkSizeX, uint chunkSizeY, uint chunkSizeZ, float invCSX, float invCSY, float invCSZ, 
            Texture2D tilemap, Texture2D illumMap, ChunkManager chunkManager)
        {
            ChunkSizeX = chunkSizeX;
            ChunkSizeY = chunkSizeY;
            ChunkSizeZ = chunkSizeZ;
            InvCSX = invCSX;
            InvCSY = invCSY;
            InvCSZ = invCSZ;
            Tilemap = tilemap;
            IllumMap = illumMap;
            this.chunkManager = chunkManager;
        }

        public ConcurrentDictionary<Point3, VoxelChunk> ChunkMap { get; set; }
        public Texture2D Tilemap { get; set; }
        public Texture2D IllumMap { get; set; }

        public int MaxChunks
        {
            get { return (int) GameSettings.Default.MaxChunks; }
            set { GameSettings.Default.MaxChunks = (ulong) value; }
        }

        public float MaxViewingLevel { get; set; }
        public ChunkManager.SliceMode Slice { get; set; }
        public Texture2D SunMap { get; set; }
        public Texture2D AmbientMap { get; set; }
        public Texture2D TorchMap { get; set; }
        public uint ChunkSizeX { get; set; }
        public uint ChunkSizeY { get; set; }
        public uint ChunkSizeZ { get; set; }
        public float InvCSX { get; set; }
        public float InvCSY { get; set; }
        public float InvCSZ { get; set; } 

        public ChunkManager ChunkManager
        {
            set { chunkManager = value; }
            get { return chunkManager; }
        }

        public void SetMaxViewingLevel(float level, ChunkManager.SliceMode slice)
        {
            Slice = slice;
            MaxViewingLevel = Math.Max(Math.Min(level, ChunkSizeY), 1);

            foreach(VoxelChunk c in ChunkMap.Select(chunks => chunks.Value).Where(c => c.NeedsViewingLevelChange()))
            {
                c.ShouldRecalculateLighting = false;
                c.ShouldRebuild = true;
            }
        }

        public Voxel GetNearestFreeAdjacentVoxel(Voxel voxel, Vector3 referenceLocation)
        {
            if(voxel == null)
            {
                return null;
            }

            if(voxel.IsEmpty)
            {
                return voxel;
            }

            List<Voxel> neighbors = voxel.Chunk.AllocateVoxels(6);
            voxel.Chunk.GetNeighborsManhattan((int)voxel.GridPosition.X, (int)voxel.GridPosition.Y, (int)voxel.GridPosition.Z, neighbors);

            Voxel closestNeighbor = null;

            float closestDist = 999;

            foreach(Voxel neighbor in neighbors)
            {
                float d = (neighbor.Position - referenceLocation).LengthSquared();

                if(d < closestDist && neighbor.IsEmpty)
                {
                    closestDist = d;
                    closestNeighbor = neighbor;
                }
            }

            return closestNeighbor;

        }

        public bool GetNeighbors(Vector3 worldPosition, List<Vector3> succ, List<Voxel> toReturn)
        {
            toReturn.Clear();
            VoxelChunk chunk = GetVoxelChunkAtWorldLocation(worldPosition);

            if (chunk == null) return false;

            Vector3 grid = chunk.WorldToGrid(worldPosition);

            chunk.GetNeighborsSuccessors(succ, (int)grid.X, (int)grid.Y, (int)grid.Z, toReturn);
            return true;
        }

        public List<Creature.MoveAction> GetMovableNeighbors(Vector3 worldPosition)
        {
            VoxelChunk chunk = GetVoxelChunkAtWorldLocation(worldPosition);

            if (chunk == null) return null;

            Vector3 grid = chunk.WorldToGrid(worldPosition);

            return chunk.GetMovableNeighbors((int)grid.X, (int)grid.Y, (int)grid.Z);
        }

        public Voxel GetFirstVisibleBlockHitByMouse(MouseState mouse, Camera camera, Viewport viewPort, bool selectEmpty = false)
        {
            Voxel vox = GetFirstVisibleBlockHitByScreenCoord(mouse.X, mouse.Y, camera, viewPort, 150.0f, false, selectEmpty);
            return vox;
        }

        public Voxel GetFirstVisibleBlockHitByScreenCoord(int x, int y, Camera camera, Viewport viewPort, float dist, bool draw = false, bool selectEmpty = false)
        {
            Vector3 pos1 = viewPort.Unproject(new Vector3(x, y, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(x, y, 1), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);
            Voxel vox = GetFirstVisibleBlockHitByRay(pos1, pos1 + dir * dist, draw, selectEmpty);

            return vox;
        }

        public bool CheckOcclusionRay(Vector3 rayStart, Vector3 rayEnd)
        {
            Voxel atPos = new Voxel();
            foreach (Point3 coord in MathFunctions.RasterizeLine(rayStart, rayEnd))
            {
                Vector3 pos = new Vector3(coord.X, coord.Y, coord.Z);

                bool success = GetNonNullVoxelAtWorldLocationCheckFirst(null, pos, ref atPos);

                if (success && atPos.IsVisible)
                {
                    return true;
                }
            }

            return false;
        }

        public bool GetFirstVoxelUnder(Vector3 rayStart, ref Voxel under, bool considerWater = false)
        {
            VoxelChunk startChunk = GetVoxelChunkAtWorldLocation(rayStart);

            if(startChunk == null)
            {
                return false;
            }

            Point3 point = new Point3(startChunk.WorldToGrid(rayStart));

            for(int y = point.Y; y >= 0; y--)
            {
                int index = startChunk.Data.IndexAt(point.X, y, point.Z);
              
                if(startChunk.Data.Types[index] != 0 || (considerWater && startChunk.Data.Water[index].WaterLevel > 0))
                {
                    under.Chunk = startChunk;
                    under.GridPosition = new Vector3(point.X, y, point.Z);
                    return true;
                }
            }

            return false;
        }

        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd)
        {
            return GetFirstVisibleBlockHitByRay(rayStart, rayEnd, null, false);
        }

        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd, bool draw, bool selectEmpty)
        {
            return GetFirstVisibleBlockHitByRay(rayStart, rayEnd, null, draw, selectEmpty);
        }


        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd, Voxel ignore,  bool draw, bool selectEmpty = false)
        {
            Vector3 delta = rayEnd - rayStart;
            float length = delta.Length();
            delta.Normalize();
            Voxel atPos = new Voxel();
            Voxel prev = new Voxel();
            foreach (Point3 coord in MathFunctions.RasterizeLine(rayStart, rayEnd))
            //for(float dn = 0.0f; dn < length; dn += 0.2f)
            {
                Vector3 pos = new Vector3(coord.X, coord.Y, coord.Z);

                bool success = GetNonNullVoxelAtWorldLocationCheckFirst(null, pos, ref atPos);
                if (draw)
                {
                    Drawer3D.DrawBox(new BoundingBox(pos, pos + new Vector3(1f, 1f, 1f)), Color.White, 0.01f);
                }
                
                if (success && atPos.IsVisible)
                {
                    return selectEmpty ? prev : atPos;
                }
                prev.Chunk = atPos.Chunk;
                prev.GridPosition = atPos.GridPosition;

            }

            return null;
        }

        public bool AddChunk(VoxelChunk chunk)
        {
            if(ChunkMap.Count < MaxChunks && !ChunkMap.ContainsKey(chunk.ID))
            {
                ChunkMap[chunk.ID] = chunk;
                return true;
            }
            else if(ChunkMap.ContainsKey(chunk.ID))
            {
                return false;
            }

            return false;
        }

        public void RemoveChunk(VoxelChunk chunk)
        {
            VoxelChunk removed = null;
            while (!ChunkMap.TryRemove(chunk.ID, out removed))
            {
                Thread.Sleep(10);
            }

            HashSet<Body> locatables = new HashSet<Body>();

            chunkManager.Components.CollisionManager.GetObjectsIntersecting(chunk.GetBoundingBox(), locatables, CollisionManager.CollisionType.Static | CollisionManager.CollisionType.Dynamic);

            foreach(Body component in locatables)
            {
                component.IsDead = true;
            }

            chunk.Destroy(chunkManager.Graphics);
        }

        public List<VoxelChunk> GetAdjacentChunks(VoxelChunk chunk)
        {
            List<VoxelChunk> toReturn = new List<VoxelChunk>();
            for(int dx = -1; dx < 2; dx++)
            {
                for(int dz = -1; dz < 2; dz++)
                {
                    if(dx != 0 || dz != 0)
                    {
                        Point3 key = new Point3(chunk.ID.X + dx, 0, chunk.ID.Z + dz);

                        if(ChunkMap.ContainsKey(key))
                        {
                            toReturn.Add(ChunkMap[key]);
                        }
                    }
                }
            }

            return toReturn;
        }

        public void RecomputeNeighbors()
        {
            foreach(VoxelChunk chunk in ChunkMap.Select(chunks => chunks.Value))
            {
                chunk.Neighbors.Clear();
            }

            foreach(KeyValuePair<Point3, VoxelChunk> chunks in ChunkMap)
            {
                VoxelChunk chunk = chunks.Value;
                List<VoxelChunk> adjacents = GetAdjacentChunks(chunk);
                foreach(VoxelChunk c in adjacents)
                {
                    if(!c.Neighbors.ContainsKey(chunk.ID) && chunk != c)
                    {
                        c.Neighbors[chunk.ID] = (chunk);
                    }
                    chunk.Neighbors[c.ID] = c;
                }
            }
        }



        public Vector3 RoundToChunkCoords(Vector3 location)
        {
            int x = MathFunctions.FloorInt(location.X * InvCSX);
            int y = MathFunctions.FloorInt(location.Y * InvCSY);
            int z = MathFunctions.FloorInt(location.Z * InvCSZ);
            return new Vector3(x, y, z);
        }

        public VoxelChunk GetVoxelChunkAtWorldLocation(Vector3 worldLocation)
        {
            Point3 id = GetChunkID(worldLocation);

            if(ChunkMap.ContainsKey(id))
            {
                return ChunkMap[id];
            }


            return null;
        }

        public bool GetVoxel(Vector3 worldLocation, ref Voxel voxel)
        {
            return GetVoxel(null, worldLocation, ref voxel);

        }

        public bool GetVoxel(VoxelChunk checkFirst, Vector3 worldLocation, ref Voxel newReference)
        {
            while(true)
            {
                if(checkFirst != null)
                {
                    if(checkFirst.IsWorldLocationValid(worldLocation))
                    {
                        if(!checkFirst.GetVoxelAtWorldLocation(worldLocation, ref newReference))
                        {
                            return false;
                        }

                        Vector3 grid = checkFirst.WorldToGrid(worldLocation);
                        newReference.Chunk = checkFirst;
                        newReference.GridPosition = new Vector3((int) grid.X, (int) grid.Y, (int) grid.Z);

                        return true;
                    }

                    checkFirst = null;
                    continue;
                }

                VoxelChunk chunk = GetVoxelChunkAtWorldLocation(worldLocation);

                if (chunk == null)
                {
                    return false;
                }


                return chunk.GetVoxelAtWorldLocation(worldLocation, ref newReference);
            }
        }

        public Point3 GetChunkID(Vector3 origin)
        {
            origin = RoundToChunkCoords(origin);
            return new Point3(MathFunctions.FloorInt(origin.X), MathFunctions.FloorInt(origin.Y), MathFunctions.FloorInt(origin.Z));
        }

        /// <summary> 
        /// Given a world location, returns the voxel at that location if it exists
        /// Otherwise returns null.
        /// </summary>
        /// <param Name="worldLocation">A floating point vector location in the world space</param>
        /// <param Name="depth">unused</param>
        /// <returns>The voxel at that location (as a list)</returns>
        public bool GetNonNullVoxelAtWorldLocation(Vector3 worldLocation, ref Voxel voxel)
        {
            return GetNonNullVoxelAtWorldLocationCheckFirst(null, worldLocation, ref voxel);
        }

        public float GetFilledVoxelGridHeightAt(float x, float y, float z)
        {
            VoxelChunk chunk = GetVoxelChunkAtWorldLocation(new Vector3(x, y, z));

            if(chunk != null)
            {
                return chunk.GetFilledVoxelGridHeightAt((int) (x - chunk.Origin.X), (int) (y - chunk.Origin.Y), (int) (z - chunk.Origin.Z));
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// TODO: Get rid of the recursion
        /// Recursive function which gets all the voxels at a position in the world, assuming the voxel is in a given chunk
        /// </summary>
        /// <param Name="checkFirst">The voxel chunk to check first</param>
        /// <param Name="worldLocation">The point in the world to check</param>
        /// <param Name="toReturn">A list of voxels to get</param>
        /// <param Name="depth">The depth of the recursion</param>
        public bool GetNonNullVoxelAtWorldLocationCheckFirst(VoxelChunk checkFirst, Vector3 worldLocation, ref Voxel toReturn)
        {

            if(checkFirst != null)
            {
                if(!checkFirst.IsWorldLocationValid(worldLocation))
                {
                    return GetNonNullVoxelAtWorldLocation(worldLocation, ref toReturn);
                }

                bool success = checkFirst.GetVoxelAtWorldLocation(worldLocation, ref toReturn);

                if(success && !toReturn.IsEmpty)
                {
                    return true;
                }
                return GetNonNullVoxelAtWorldLocation(worldLocation, ref toReturn);
            }

            VoxelChunk chunk = GetVoxelChunkAtWorldLocation(worldLocation);

            if(chunk == null)
            {
                return false;
            }

            if(!chunk.IsWorldLocationValid(worldLocation))
            {
                return false;
            }

            if (chunk.GetVoxelAtWorldLocation(worldLocation, ref toReturn) && !toReturn.IsEmpty)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<LiquidPrimitive> GetAllLiquidPrimitives()
        {
            List<LiquidPrimitive> toReturn = new List<LiquidPrimitive>();

            foreach(VoxelChunk chunk in ChunkMap.Select(chunks => chunks.Value))
            {
                toReturn.AddRange(chunk.Liquids.Values);
            }

            return toReturn;
        }

        public bool DoesWaterCellExist(Vector3 worldLocation)
        {
            VoxelChunk chunkAtLocation = GetVoxelChunkAtWorldLocation(worldLocation);
            return chunkAtLocation != null && chunkAtLocation.IsWorldLocationValid(worldLocation);
        }

        public WaterCell GetWaterCellAtLocation(Vector3 worldLocation)
        {
            VoxelChunk chunkAtLocation = GetVoxelChunkAtWorldLocation(worldLocation);

            if(chunkAtLocation == null)
            {
                return new WaterCell();
            }

            Vector3 gridPos = chunkAtLocation.WorldToGrid(worldLocation);
            return chunkAtLocation.Data.Water[chunkAtLocation.Data.IndexAt((int) gridPos.X, (int) gridPos.Y, (int) gridPos.Z)];
        }

        public bool SaveAllChunks(string directory, bool compress)
        {
            foreach(KeyValuePair<Point3, VoxelChunk> pair in ChunkMap)
            {
                ChunkFile chunkFile = new ChunkFile(pair.Value);

                string fileName = directory + Path.DirectorySeparatorChar + pair.Key.X + "_" + pair.Key.Y + "_" + pair.Key.Z;

                if(compress)
                {
                    fileName += ".zch";
                }
                else
                {
                    fileName += ".jch";
                }

                if(!chunkFile.WriteFile(fileName, compress))
                {
                    return false;
                }
            }

            return true;
        }

        public void LoadFromFile(GameFile gameFile, ref string loadingMessage)
        {
            foreach(VoxelChunk chunk in gameFile.Data.ChunkData.Select(file => file.ToChunk(chunkManager)))
            {
                AddChunk(chunk);
            }
            chunkManager.UpdateBounds();
            chunkManager.UpdateRebuildList();
            chunkManager.CreateGraphics(ref loadingMessage, this);
        }
    }

}