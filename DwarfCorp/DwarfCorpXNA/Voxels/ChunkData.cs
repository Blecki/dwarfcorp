// ChunkData.cs
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
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
            ChunkManager chunkManager)
        {
            ChunkSizeX = chunkSizeX;
            ChunkSizeY = chunkSizeY;
            ChunkSizeZ = chunkSizeZ;
            InvCSX = invCSX;
            InvCSY = invCSY;
            InvCSZ = invCSZ;
            this.chunkManager = chunkManager;
        }

        public ConcurrentDictionary<GlobalChunkCoordinate, VoxelChunk> ChunkMap { get; set; }

        public Texture2D Tilemap
        {
            get { return TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles); }
        }

        public Texture2D IllumMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Terrain.terrain_illumination); }
        }

        public int MaxChunks
        {
            get { return (int) GameSettings.Default.MaxChunks; }
            set { GameSettings.Default.MaxChunks = (int) value; }
        }

        public float MaxViewingLevel { get; set; }
        public ChunkManager.SliceMode Slice { get; set; }

        public Texture2D SunMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Gradients.sungradient); }
        }

        public Texture2D AmbientMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Gradients.ambientgradient); }
        }

        public Texture2D TorchMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Gradients.torchgradient); }
        }

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
            if (Math.Abs(level - MaxViewingLevel) < 0.1f && slice == Slice)
            {
                return;
            }

            Slice = slice;
            MaxViewingLevel = Math.Max(Math.Min(level, ChunkSizeY), 1);

            foreach (VoxelChunk c in ChunkMap.Select(chunks => chunks.Value))
            {
                c.ShouldRecalculateLighting = true;
                c.ShouldRebuild = true;
            }
        }

        public void Reveal(VoxelHandle voxel)
        {
            Reveal(new List<VoxelHandle>() {voxel});
        }

        public void Reveal(IEnumerable<VoxelHandle> voxels)
        {
            if (!GameSettings.Default.FogofWar) return;

            var affectedChunks = new List<GlobalChunkCoordinate>();
            Queue<VoxelHandle> q = new Queue<VoxelHandle>(128);

            foreach (VoxelHandle voxel in voxels)
            {
                if (voxel != null)
                    q.Enqueue(voxel);
            }
            List<VoxelHandle> neighbors = new List<VoxelHandle>();
            VoxelHandle currNeighbor = new VoxelHandle();
            while (q.Count > 0)
            {
                VoxelHandle v = q.Dequeue();
                if (v == null) continue;

                if (!affectedChunks.Contains(v.ChunkID))
                {
                    affectedChunks.Add(v.ChunkID);
                }
                v.Chunk.GetNeighborsManhattan(v, neighbors);
                foreach (VoxelHandle nextVoxel in neighbors)
                {
                    if (nextVoxel == null) continue;

                    if (nextVoxel.IsExplored) continue;

                    nextVoxel.Chunk.NotifyExplored(nextVoxel.GridPosition);

                    nextVoxel.IsExplored = true;
                    if (!affectedChunks.Contains(nextVoxel.ChunkID))
                    {
                        affectedChunks.Add(nextVoxel.ChunkID);
                    }
                    if (nextVoxel.IsEmpty)
                        q.Enqueue(new VoxelHandle(nextVoxel.GridPosition, nextVoxel.Chunk));

                }

                v.IsExplored = true;

            }

            foreach (var chunkID in affectedChunks)
            {
                VoxelChunk chunk = ChunkMap[chunkID];

                if (!chunk.ShouldRebuild)
                {
                    chunk.ShouldRecalculateLighting = true;
                    chunk.ShouldRebuildWater = true;
                    chunk.ShouldRebuild = true;
                }
            }

        }

        public VoxelHandle GetNearestFreeAdjacentVoxel(VoxelHandle voxel, Vector3 referenceLocation)
        {
            if (voxel == null)
            {
                return null;
            }

            if (voxel.IsEmpty)
            {
                return voxel;
            }

            List<VoxelHandle> neighbors = voxel.Chunk.AllocateVoxels(6);
            voxel.Chunk.GetNeighborsManhattan((int) voxel.GridPosition.X, (int) voxel.GridPosition.Y,
                (int) voxel.GridPosition.Z, neighbors);

            VoxelHandle closestNeighbor = null;

            float closestDist = 999;

            foreach (VoxelHandle neighbor in neighbors)
            {
                float d = (neighbor.WorldPosition - referenceLocation).LengthSquared();

                if (d < closestDist && neighbor.IsEmpty)
                {
                    closestDist = d;
                    closestNeighbor = neighbor;
                }
            }

            return closestNeighbor;

        }

        public bool GetNeighbors(GlobalVoxelCoordinate worldPosition, List<GlobalVoxelOffset> succ, List<VoxelHandle> toReturn)
        {
            toReturn.Clear();
            VoxelChunk chunk = GetVoxelChunkAtWorldLocation(worldPosition);

            if (chunk == null) return false;

            var grid = worldPosition.GetLocalVoxelCoordinate();
            chunk.GetNeighborsSuccessors(succ, grid.X, grid.Y, grid.Z, toReturn);
            return true;
        }


        public VoxelHandle GetFirstVisibleBlockHitByMouse(MouseState mouse, Camera camera, Viewport viewPort,
            bool selectEmpty = false, Func<VoxelHandle, bool> acceptFn = null)
        {
            VoxelHandle vox = GetFirstVisibleBlockHitByScreenCoord(mouse.X, mouse.Y, camera, viewPort, 150.0f, false,
                selectEmpty, acceptFn);
            return vox;
        }

        public VoxelHandle GetFirstVisibleBlockHitByScreenCoord(int x, int y, Camera camera, Viewport viewPort, float dist,
            bool draw = false, bool selectEmpty = false, Func<VoxelHandle, bool> acceptFn = null)
        {
            Vector3 pos1 = viewPort.Unproject(new Vector3(x, y, 0), camera.ProjectionMatrix, camera.ViewMatrix,
                Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(x, y, 1), camera.ProjectionMatrix, camera.ViewMatrix,
                Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);
            VoxelHandle vox = GetFirstVisibleBlockHitByRay(pos1, pos1 + dir*dist, draw, selectEmpty, acceptFn);

            return vox;
        }

        public bool CheckRaySolid(Vector3 rayStart, Vector3 rayEnd)
        {
            VoxelHandle atPos = new VoxelHandle();
            foreach (Point3 coord in MathFunctions.RasterizeLine(rayStart, rayEnd))
            {
                Vector3 pos = new Vector3(coord.X, coord.Y, coord.Z);

                bool success = GetNonNullVoxelAtWorldLocationCheckFirst(null, pos, ref atPos);

                if (success && !atPos.IsEmpty)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsVoxelVisibleSurface(VoxelHandle voxel)
        {
            if (voxel == null) return false;

            if (!voxel.IsVisible || voxel.IsEmpty) return false;

            List<VoxelHandle> neighbors = new List<VoxelHandle>(6);
            voxel.Chunk.GetNeighborsManhattan(voxel, neighbors);

            foreach (VoxelHandle neighbor in neighbors)
            {
                if (neighbor == null || (neighbor.IsEmpty && neighbor.IsExplored) || !(neighbor.IsVisible))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsVoxelOccluded(VoxelHandle voxel, Vector3 cameraPos)
        {
            Vector3 voxelPoint = voxel.WorldPosition + Vector3.One * 0.5f;
            VoxelHandle atPos = new VoxelHandle();
            foreach (Point3 coord in MathFunctions.RasterizeLine(cameraPos, voxelPoint))
            {
                Vector3 pos = new Vector3(coord.X, coord.Y, coord.Z);

                bool success = GetNonNullVoxelAtWorldLocationCheckFirst(null, pos, ref atPos);

                if (success && atPos.IsVisible && !atPos.Equals(voxel))
                {
                    return true;
                }
            }
            return false;
        }

        public bool CheckOcclusionRay(Vector3 rayStart, Vector3 rayEnd)
        {
            VoxelHandle atPos = new VoxelHandle();
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

        public bool GetFirstVoxelAbove(GlobalVoxelCoordinate position, ref VoxelHandle under, bool considerWater = false)
        {
            VoxelChunk startChunk = GetVoxelChunkAtWorldLocation(position);

            if (startChunk == null)
            {
                return false;
            }

            var point = position.GetLocalVoxelCoordinate();

            for (int y = point.Y; y < ChunkSizeY; y++)
            {
                int index = VoxelData.IndexAt(new LocalVoxelCoordinate(point.X, y, point.Z));

                if (startChunk.Data.Types[index] != 0 || (considerWater && startChunk.Data.Water[index].WaterLevel > 0))
                {
                    under.Chunk = startChunk;
                    under.GridPosition = new LocalVoxelCoordinate(point.X, y, point.Z);
                    return true;
                }
            }

            return false;
        }

        // Todo: %KILL% Is raystart a real ray? Probably not.
        public bool GetFirstVoxelUnder(Vector3 rayStart, ref VoxelHandle under, bool considerWater = false)
        {
            VoxelChunk startChunk = GetVoxelChunkAtWorldLocation(
                new GlobalVoxelCoordinate((int)rayStart.X, (int)rayStart.Y, (int)rayStart.Z));

            if(startChunk == null)
            {
                return false;
            }

            Point3 point = new Point3(startChunk.WorldToGrid(rayStart));

            for(int y = point.Y; y >= 0; y--)
            {
                int index = VoxelData.IndexAt(new LocalVoxelCoordinate(point.X, y, point.Z));
              
                if(startChunk.Data.Types[index] != 0 || (considerWater && startChunk.Data.Water[index].WaterLevel > 0))
                {
                    under.Chunk = startChunk;
                    under.GridPosition = new LocalVoxelCoordinate(point.X, y, point.Z);
                    return true;
                }
            }

            return false;
        }

        public VoxelHandle GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd)
        {
            return GetFirstVisibleBlockHitByRay(rayStart, rayEnd, null, false);
        }

        public VoxelHandle GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd, bool draw, bool selectEmpty, Func<VoxelHandle, bool> acceptFn = null)
        {
            return GetFirstVisibleBlockHitByRay(rayStart, rayEnd, null, draw, selectEmpty, acceptFn);
        }


        public VoxelHandle GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd, VoxelHandle ignore,  bool draw, bool selectEmpty = false, Func<VoxelHandle, bool> acceptFn = null)
        {
            if (acceptFn == null)
            {
                acceptFn = v => v != null && !v.IsEmpty;
            }
            Vector3 delta = rayEnd - rayStart;
            float length = delta.Length();
            delta.Normalize();
            VoxelHandle atPos = new VoxelHandle();
            VoxelHandle prev = new VoxelHandle();
            foreach (Point3 coord in MathFunctions.RasterizeLine(rayStart, rayEnd))
            //for(float dn = 0.0f; dn < length; dn += 0.2f)
            {
                Vector3 pos = new Vector3(coord.X, coord.Y, coord.Z);

                bool success = GetVoxel(pos, ref atPos) && acceptFn(atPos);
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

            HashSet<IBoundedObject> locatables = new HashSet<IBoundedObject>();

            chunkManager.World.CollisionManager.GetObjectsIntersecting(chunk.GetBoundingBox(), locatables, CollisionManager.CollisionType.Static | CollisionManager.CollisionType.Dynamic);

            foreach(var component in locatables.Where(o => o is Body).Select(o => o as Body))
            {
                component.Die();
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
                        var key = new GlobalChunkCoordinate(chunk.ID.X + dx, 0, chunk.ID.Z + dz);

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
            foreach (VoxelChunk chunk in ChunkMap.Select(chunks => chunks.Value))
            {
                chunk.Neighbors.Clear();
                chunk.EuclidianNeighbors.Clear();
            }

            foreach (var chunks in ChunkMap)
            {
                VoxelChunk chunk = chunks.Value;

                Point3 successor = new Point3(0, 0, 0);
                for (successor.X = -1; successor.X < 2; successor.X++)
                {
                    for (successor.Z = -1; successor.Z < 2; successor.Z++)
                    {
                        for (successor.Y = -1; successor.Y < 2; successor.Y++)
                        {
                            var sideChunkID = new GlobalChunkCoordinate(
                                chunk.ID.X + successor.X,
                                chunk.ID.Y + successor.Y,
                                chunk.ID.Z + successor.Z);
                            VoxelChunk sideChunk;
                            ChunkMap.TryGetValue(sideChunkID, out sideChunk);
                            if (successor.Y == 0 && sideChunk != null)
                            {
                                if (!sideChunk.Neighbors.ContainsKey(chunk.ID) && chunk != sideChunk)
                                    chunk.Neighbors[chunk.ID] = chunk;
                                chunk.Neighbors[sideChunkID] = sideChunk;
                            }
                            chunk.EuclidianNeighbors[VoxelChunk.SuccessorToEuclidianLookupKey(successor)] = sideChunk;
                        }
                    }
                }
            }
        }



        public GlobalChunkCoordinate RoundToChunkCoords(Vector3 location)
        {
            int x = MathFunctions.FloorInt(location.X * InvCSX);
            int y = MathFunctions.FloorInt(location.Y * InvCSY);
            int z = MathFunctions.FloorInt(location.Z * InvCSZ);
            return new GlobalChunkCoordinate(x, y, z);
        }

        public GlobalChunkCoordinate RoundToChunkCoordsPoint3(Vector3 location)
        {
            int x = MathFunctions.FloorInt(location.X * InvCSX);
            int y = MathFunctions.FloorInt(location.Y * InvCSY);
            int z = MathFunctions.FloorInt(location.Z * InvCSZ);
            return new GlobalChunkCoordinate(x, y, z);
        }

        public VoxelChunk GetVoxelChunkAtWorldLocation(GlobalVoxelCoordinate worldLocation)
        {
            VoxelChunk returnChunk = null;
            ChunkMap.TryGetValue(worldLocation.GetGlobalChunkCoordinate(), out returnChunk);
            return returnChunk;
        }

        public VoxelChunk GetChunk(Vector3 WorldLocation)
        {
            return GetVoxelChunkAtWorldLocation(new GlobalVoxelCoordinate(
                (int)Math.Floor(WorldLocation.X),
                (int)Math.Floor(WorldLocation.Y),
                (int)Math.Floor(WorldLocation.Z)));
        }

        public bool GetVoxel(Vector3 WorldLocation, ref VoxelHandle Voxel)
        {
            return GetVoxel(null, new GlobalVoxelCoordinate(
                (int)Math.Floor(WorldLocation.X),
                (int)Math.Floor(WorldLocation.Y),
                (int)Math.Floor(WorldLocation.Z)), ref Voxel);
        }

        public bool GetVoxel(GlobalVoxelCoordinate worldLocation, ref VoxelHandle voxel)
        {
            return GetVoxel(null, worldLocation, ref voxel);
        }

        public bool GetVoxel(VoxelChunk checkFirst, GlobalVoxelCoordinate worldLocation, ref VoxelHandle newReference)
        {
            var chunk = GetVoxelChunkAtWorldLocation(worldLocation);
            if (chunk == null) return false;
            return chunk.GetVoxelAtValidWorldLocation(worldLocation, ref newReference);
        }

        public GlobalChunkCoordinate GetChunkID(Vector3 origin)
        {
            return RoundToChunkCoordsPoint3(origin);
        }

        /// <summary> 
        /// Given a world location, returns the voxel at that location if it exists
        /// Otherwise returns null.
        /// </summary>
        /// <param Name="worldLocation">A floating point vector location in the world space</param>
        /// <param Name="depth">unused</param>
        /// <returns>The voxel at that location (as a list)</returns>
        public bool GetNonNullVoxelAtWorldLocation(Vector3 worldLocation, ref VoxelHandle voxel)
        {
            return GetNonNullVoxelAtWorldLocationCheckFirst(null, worldLocation, ref voxel);
        }

        public float GetFilledVoxelGridHeightAt(float x, float y, float z)
        {
            var chunk = GetChunk(new Vector3(x, y, z));

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
        public bool GetNonNullVoxelAtWorldLocationCheckFirst(VoxelChunk checkFirst, Vector3 worldLocation, ref VoxelHandle toReturn)
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

            var chunk = GetChunk(worldLocation);

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
        /*
        public List<LiquidPrimitive> GetAllLiquidPrimitives()
        {
            List<LiquidPrimitive> toReturn = new List<LiquidPrimitive>();

            foreach(VoxelChunk chunk in ChunkMap.Select(chunks => chunks.Value))
            {
                toReturn.AddRange(chunk.Liquids.Values);
            }

            return toReturn;
        }*/

        public bool DoesWaterCellExist(Vector3 worldLocation)
        {
            var chunkAtLocation = GetChunk(worldLocation);
            return chunkAtLocation != null && chunkAtLocation.IsWorldLocationValid(worldLocation);
        }

        //Todo: %KILL%
        public WaterCell GetWaterCellAtLocation(GlobalVoxelCoordinate worldLocation)
        {
            var chunkAtLocation = GetVoxelChunkAtWorldLocation(worldLocation);

            if(chunkAtLocation == null)
            {
                return new WaterCell();
            }

            return chunkAtLocation.Data.Water[VoxelData.IndexAt(worldLocation.GetLocalVoxelCoordinate())];
        }

        public bool SaveAllChunks(string directory, bool compress)
        {
            foreach(var pair in ChunkMap)
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

                if(!chunkFile.WriteFile(fileName, compress, true))
                {
                    return false;
                }
            }

            return true;
        }

        public void LoadFromFile(GameFile gameFile, Action<String> SetLoadingMessage)
        {
            foreach (VoxelChunk chunk in gameFile.Data.ChunkData.Select(file => file.ToChunk(chunkManager)))
            {
                AddChunk(chunk);
            }
            RecomputeNeighbors();
            chunkManager.UpdateBounds();
            chunkManager.CreateGraphics(SetLoadingMessage, this);
        }


    }

}
