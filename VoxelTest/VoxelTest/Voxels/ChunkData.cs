using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public ChunkData(uint chunkSizeX, uint chunkSizeY, uint chunkSizeZ, float invCSX, float invCSY, float invCSZ, Texture2D tilemap, Texture2D illumMap, ChunkManager chunkManager)
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

        public VoxelRef GetNearestFreeAdjacentVoxel(VoxelRef voxel, Vector3 referenceLocation)
        {
            if(voxel == null)
            {
                return null;
            }

            if(voxel.TypeName == "empty")
            {
                return voxel;
            }

            List<VoxelRef> neighbors = ChunkMap[voxel.ChunkID].GetNeighborsManhattan((int)voxel.GridPosition.X, (int)voxel.GridPosition.Y, (int)voxel.GridPosition.Z);

            VoxelRef closestNeighbor = null;

            float closestDist = 999;

            foreach(VoxelRef neighbor in neighbors)
            {
                float d = (neighbor.WorldPosition - referenceLocation).LengthSquared();

                if(d < closestDist && neighbor.TypeName == "empty")
                {
                    closestDist = d;
                    closestNeighbor = neighbor;
                }
            }

            return closestNeighbor;

        }

        public Ray GetMouseRay(MouseState mouse, Camera camera, Viewport viewPort)
        {
            float x = mouse.X;
            float y = mouse.Y;
            Vector3 pos1 = viewPort.Unproject(new Vector3(x, y, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(x, y, 1), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);
            return new Ray(pos1, dir);
        }

        public Voxel GetFirstVisibleBlockHitByMouse(MouseState mouse, Camera camera, Viewport viewPort)
        {
            Voxel vox = GetFirstVisibleBlockHitByScreenCoord(mouse.X, mouse.Y, camera, viewPort, 50.0f);
            return vox;
        }

        public Voxel GetFirstVisibleBlockHitByScreenCoord(int x, int y, Camera camera, Viewport viewPort, float dist)
        {
            Vector3 pos1 = viewPort.Unproject(new Vector3(x, y, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(x, y, 1), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);
            Voxel vox = GetFirstVisibleBlockHitByRay(pos1, pos1 + dir * dist, false);

            return vox;
        }

        public Voxel GetFirstVoxelUnder(Vector3 rayStart)
        {
            VoxelChunk startChunk = GetVoxelChunkAtWorldLocation(rayStart);

            if(startChunk == null)
            {
                return null;
            }

            Point3 point = new Point3(startChunk.WorldToGrid(rayStart));

            for(int y = point.Y; y >= 0; y--)
            {
                Voxel vox = startChunk.VoxelGrid[point.X][y][point.Z];
                if(vox != null)
                {
                    return vox;
                }
            }

            return null;
        }

        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd)
        {
            VoxelChunk startChunk = GetVoxelChunkAtWorldLocation(rayStart);

            return GetFirstVisibleBlockHitByRay(rayStart, rayEnd, null, startChunk, false);
        }

        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd, bool draw)
        {
            VoxelChunk startChunk = GetVoxelChunkAtWorldLocation(rayStart);

            return GetFirstVisibleBlockHitByRay(rayStart, rayEnd, null, startChunk, draw);
        }

        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd, Voxel ignore, VoxelChunk startChunk, bool draw)
        {
            Vector3 delta = rayEnd - rayStart;
            float length = delta.Length();
            delta.Normalize();


            for(float dn = 0.0f; dn < length; dn += 0.2f)
            {
                Vector3 pos = rayStart + delta * dn;

                Voxel atPos = GetNonNullVoxelAtWorldLocationCheckFirst(startChunk, pos);

                if(draw && atPos != null)
                {
                    Drawer3D.DrawBox(new BoundingBox(pos, pos + new Vector3(0.01f, 0.01f, 0.01f)), Color.White, 0.01f);
                }

                if(atPos != null && atPos.IsVisible)
                {
                    return atPos;
                }

           
            }

            return null;
        }

        public void AddChunk(VoxelChunk chunk)
        {
            if(ChunkMap.Count < MaxChunks && !ChunkMap.ContainsKey(chunk.ID))
            {
                ChunkMap[chunk.ID] = chunk;
                chunkManager.ChunkOctree.AddObjectRecursive(chunk);
            }
            else if(ChunkMap.ContainsKey(chunk.ID))
            {
                RemoveChunk(ChunkMap[chunk.ID]);
                ChunkMap[chunk.ID] = chunk;
                chunkManager.ChunkOctree.AddObjectRecursive(chunk);
            }
        }

        public void RemoveChunk(VoxelChunk chunk)
        {
            VoxelChunk removed = null;
            while(!ChunkMap.TryRemove(chunk.ID, out removed))
            {
                chunkManager.ChunkOctree.Root.RemoveObject(chunk);
            }

            HashSet<LocatableComponent> locatables = new HashSet<LocatableComponent>();

            chunkManager.Components.CollisionManager.GetObjectsIntersecting(chunk.GetBoundingBox(), locatables, CollisionManager.CollisionType.Static | CollisionManager.CollisionType.Dynamic);

            foreach(LocatableComponent component in locatables)
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
            int x = (int) (location.X * InvCSX);
            int y = (int) (location.Y * InvCSY);
            int z = (int) (location.Z * InvCSZ);
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

        public VoxelRef GetVoxelReferenceAtWorldLocation(Vector3 worldLocation)
        {
            return GetVoxelReferenceAtWorldLocation(null, worldLocation);

        }

        public VoxelRef GetVoxelReferenceAtWorldLocation(VoxelChunk checkFirst, Vector3 worldLocation)
        {
            while(true)
            {
                Vector3 grid;
                VoxelRef newReference;
                if(checkFirst != null)
                {
                    if(checkFirst.IsWorldLocationValid(worldLocation))
                    {
                        Voxel v = checkFirst.GetVoxelAtWorldLocation(worldLocation);

                        if(v != null)
                        {
                            return v.GetReference();
                        }

                        grid = checkFirst.WorldToGrid(worldLocation);
                        newReference = new VoxelRef
                        {
                            ChunkID = checkFirst.ID,
                            GridPosition = new Vector3((int) grid.X, (int) grid.Y, (int) grid.Z)
                        };
                        newReference.WorldPosition = newReference.GridPosition + checkFirst.Origin;
                        newReference.TypeName = "empty";
                        newReference.IsValid = true;
                        return newReference;
                    }

                    checkFirst = null;
                    continue;
                }

                VoxelChunk chunk = GetVoxelChunkAtWorldLocation(worldLocation);

                if (chunk == null)
                {
                    return null;
                }

                Voxel got = chunk.GetVoxelAtWorldLocation(worldLocation);

                if(got != null)
                {
                    return got.GetReference();
                }

                grid = chunk.WorldToGrid(worldLocation);
                newReference = new VoxelRef
                {
                    ChunkID = chunk.ID,
                    GridPosition = new Vector3((int) grid.X, (int) grid.Y, (int) grid.Z),
                    TypeName = "empty",
                    IsValid = true
                };
                newReference.WorldPosition = newReference.GridPosition + chunk.Origin;
                return newReference;
            }
        }

        public Point3 GetChunkID(Vector3 origin)
        {
            origin = RoundToChunkCoords(origin);
            return new Point3((int) origin.X, (int) origin.Y, (int) origin.Z);
        }

        /// <summary> 
        /// Given a world location, returns the voxel at that location if it exists
        /// Otherwise returns null.
        /// </summary>
        /// <param Name="worldLocation">A floating point vector location in the world space</param>
        /// <param Name="depth">unused</param>
        /// <returns>The voxel at that location (as a list)</returns>
        public Voxel GetNonNullVoxelAtWorldLocation(Vector3 worldLocation)
        {
            return GetNonNullVoxelAtWorldLocationCheckFirst(null, worldLocation);
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
        public Voxel GetNonNullVoxelAtWorldLocationCheckFirst(VoxelChunk checkFirst, Vector3 worldLocation)
        {

            if(checkFirst != null)
            {
                if(!checkFirst.IsWorldLocationValid(worldLocation))
                {
                    return GetNonNullVoxelAtWorldLocation(worldLocation);
                }

                Voxel v = checkFirst.GetVoxelAtWorldLocation(worldLocation);

                if(v != null)
                {
                    return v;
                }
                return GetNonNullVoxelAtWorldLocation(worldLocation);
            }

            VoxelChunk chunk = GetVoxelChunkAtWorldLocation(worldLocation);

            if(chunk == null)
            {
                return null;
            }

            if(!chunk.IsWorldLocationValid(worldLocation))
            {
                return null;
            }

            Voxel got = chunk.GetVoxelAtWorldLocation(worldLocation);
            return got ?? null;
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
                return null;
            }

            Vector3 gridPos = chunkAtLocation.WorldToGrid(worldLocation);
            return chunkAtLocation.Water[(int) gridPos.X][(int) gridPos.Y][(int) gridPos.Z];
        }

        public List<VoxelRef> GetVoxelsIntersecting(BoundingBox box)
        {
            HashSet<VoxelChunk> intersects = new HashSet<VoxelChunk>();
            chunkManager.ChunkOctree.Root.GetComponentsIntersecting<VoxelChunk>(box, intersects);

            List<VoxelRef> toReturn = new List<VoxelRef>();

            foreach(VoxelChunk chunk in intersects)
            {
                toReturn.AddRange(chunk.GetVoxelsIntersecting(box));
            }

            return toReturn;
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


            
            chunkManager.UpdateRebuildList();
            chunkManager.CreateGraphics(ref loadingMessage, this);
        }
    }

}