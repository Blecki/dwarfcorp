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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// Has a collection of voxel chunks, and methods for accessing them.
    /// chunks are stored in a grid right next to each other, and contain a
    /// fixed number of voxels per chunk.
    /// </summary>
    public class ChunkData
    {
        #region properties
        /// <summary>
        /// The chunk manager
        /// </summary>
        private ChunkManager chunkManager;

        /// <summary>
        /// Gets or sets the chunk map.
        /// </summary>
        /// <value>
        /// A dictionary of chunk origins (in chunk coordinates) to chunks.
        /// </value>
        public ConcurrentDictionary<Point3, VoxelChunk> ChunkMap { get; set; }

        /// <summary>
        /// Gets the tilemap texture.
        /// </summary>
        /// <value>
        /// The tilemap.
        /// </value>
        public Texture2D Tilemap
        {
            get { return TextureManager.GetTexture(ContentPaths.Terrain.terrain_tiles); }
        }

        /// <summary>
        /// Gets the texture self-illumination map.
        /// </summary>
        /// <value>
        /// The self-illumination texture.
        /// </value>
        public Texture2D IllumMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Terrain.terrain_illumination); }
        }

        /// <summary>
        /// Gets or sets the maximum number of chunks.
        /// </summary>
        /// <value>
        /// The maximum number of chunks.
        /// </value>
        public int MaxChunks
        {
            get { return GameSettings.Default.MaxChunks; }
            set { GameSettings.Default.MaxChunks = value; }
        }

        /// <summary>
        /// Gets or sets the maximum viewing level (in y)
        /// all voxlels above this level will be invisible.
        /// </summary>
        /// <value>
        /// The maximum viewing level.
        /// </value>
        public float MaxViewingLevel { get; set; }

        /// <summary>
        /// Gets or sets the slice mode (X, Y or Z)
        /// </summary>
        /// <value>
        /// The slice mode.
        /// </value>
        public ChunkManager.SliceMode Slice { get; set; }

        /// <summary>
        /// Gets the sunlight color ramp.
        /// </summary>
        /// <value>
        /// The sunlight color ramp.
        /// </value>
        public Texture2D SunMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Gradients.sungradient); }
        }

        /// <summary>
        /// Gets the ambient light color ramp.
        /// </summary>
        /// <value>
        /// The ambient light color ramp.
        /// </value>
        public Texture2D AmbientMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Gradients.ambientgradient); }
        }

        /// <summary>
        /// Gets the torch light color ramp.
        /// </summary>
        /// <value>
        /// The torch light color ramp.
        /// </value>
        public Texture2D TorchMap
        {
            get { return TextureManager.GetTexture(ContentPaths.Gradients.torchgradient); }
        }

        /// <summary>
        /// Gets or sets the number of voxels per chunk in x.
        /// </summary>
        /// <value>
        /// The chunk size x.
        /// </value>
        public uint ChunkSizeX { get; set; }
        /// <summary>
        /// Gets or sets the number of voxels per chunk in y.
        /// </summary>
        /// <value>
        /// The chunk size y.
        /// </value>
        public uint ChunkSizeY { get; set; }
        /// <summary>
        /// Gets or sets the number of voxels per chunk in z.
        /// </summary>
        /// <value>
        /// The chunk size z.
        /// </value>
        public uint ChunkSizeZ { get; set; }

        /// <summary>
        /// 1.0 / ChunkSizeX
        /// </summary>
        public float InvCSX { get; set; }
        /// <summary>
        /// 1.0 / ChunkSizeY
        /// </summary>
        public float InvCSY { get; set; }
        /// <summary>
        /// 1.0 / ChunkSizeZ
        /// </summary>
        public float InvCSZ { get; set; }

        /// <summary>
        /// Gets or sets the chunk manager.
        /// </summary>
        /// <value>
        /// The chunk manager.
        /// </value>
        public ChunkManager ChunkManager
        {
            set { chunkManager = value; }
            get { return chunkManager; }
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkData"/> class.
        /// </summary>
        /// <param name="chunkSizeX">The number of voxels in x.</param>
        /// <param name="chunkSizeY">The number of voxels in y</param>
        /// <param name="chunkSizeZ">The number of voxels in z</param>
        /// <param name="invCSX">The inverse of the number of voxels in x</param>
        /// <param name="invCSY">The inverse of the number of voxels in y</param>
        /// <param name="invCSZ">The iverse of the number of voxels in z</param>
        /// <param name="chunkManager">The chunk manager.</param>
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

        /// <summary>
        /// Sets the maximum height at which voxels will be visible.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="slice">The slice mode</param>
        public void SetMaxViewingLevel(float level, ChunkManager.SliceMode slice)
        {
            Slice = slice;
            MaxViewingLevel = Math.Max(Math.Min(level, ChunkSizeY), 1);

            foreach (VoxelChunk c in ChunkMap.Select(chunks => chunks.Value))
            {
                c.ShouldRecalculateLighting = false;
                c.ShouldRebuild = true;
            }
        }

        /// <summary>
        /// Reveals the specified voxel, filling out the chunks that have been affected by this
        /// reveal through flood filling.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <param name="affectedChunks">The affected chunks.</param>
        public void Reveal(Voxel voxel, HashSet<VoxelChunk> affectedChunks)
        {
            Reveal(new List<Voxel> {voxel}, affectedChunks);
        }

        /// <summary>
        /// Reveals the specified voxels, filling out the chunks that have been affected by this
        /// reveal through flood filling.
        /// </summary>
        /// <param name="voxels">The voxels.</param>
        /// <param name="affectedChunks">The affected chunks.</param>
        public void Reveal(IEnumerable<Voxel> voxels, HashSet<VoxelChunk> affectedChunks)
        {
            if (!GameSettings.Default.FogofWar) return;
            var q = new Queue<Voxel>(128);

            foreach (Voxel voxel in voxels)
            {
                if (voxel != null)
                    q.Enqueue(voxel);
            }
            var neighbors = new List<Voxel>();
            while (q.Count > 0)
            {
                Voxel v = q.Dequeue();
                if (v == null) continue;
                if (!affectedChunks.Contains(v.Chunk))
                {
                    affectedChunks.Add(v.Chunk);
                }
                v.Chunk.GetNeighborsManhattan(v, neighbors);
                foreach (Voxel nextVoxel in neighbors)
                {
                    if (nextVoxel == null) continue;

                    if (nextVoxel.IsExplored) continue;

                    nextVoxel.Chunk.NotifyExplored(new Point3(nextVoxel.GridPosition));

                    if (!nextVoxel.IsEmpty)
                    {
                        if (!nextVoxel.IsExplored)
                        {
                            nextVoxel.IsExplored = true;
                            if (!affectedChunks.Contains(nextVoxel.Chunk))
                            {
                                affectedChunks.Add(nextVoxel.Chunk);
                            }
                        }
                        continue;
                    }

                    if (!v.IsExplored)
                        q.Enqueue(new Voxel(new Point3(nextVoxel.GridPosition), nextVoxel.Chunk));
                }
                v.IsExplored = true;
            }
        }

        /// <summary>
        /// Gets the nearest empty adjacent voxel to a location.
        /// </summary>
        /// <param name="voxel">The voxel to consider neighbors of.</param>
        /// <param name="referenceLocation">The reference location. Voxels are sorted 
        /// in ascending order relative to this reference location.</param>
        /// <returns>The empty voxel which is a) adjacent to the passed in voxel and b) nearest to the reference
        /// location, or null if no such voxel exists.</returns>
        public Voxel GetNearestFreeAdjacentVoxel(Voxel voxel, Vector3 referenceLocation)
        {
            if (voxel == null)
            {
                return null;
            }

            if (voxel.IsEmpty)
            {
                return voxel;
            }

            List<Voxel> neighbors = voxel.Chunk.AllocateVoxels(6);
            voxel.Chunk.GetNeighborsManhattan((int) voxel.GridPosition.X, (int) voxel.GridPosition.Y,
                (int) voxel.GridPosition.Z, neighbors);

            Voxel closestNeighbor = null;

            float closestDist = 999;

            foreach (Voxel neighbor in neighbors)
            {
                float d = (neighbor.Position - referenceLocation).LengthSquared();

                if (d < closestDist && neighbor.IsEmpty)
                {
                    closestDist = d;
                    closestNeighbor = neighbor;
                }
            }

            return closestNeighbor;
        }

        /// <summary>
        /// Gets the neighbors of the voxel at the specified world position.
        /// </summary>
        /// <param name="worldPosition">The world position.</param>
        /// <param name="succ">List of successors (relative coordinates) to consider.</param>
        /// <param name="toReturn">The list of voxels adjacent to the one at the given position.</param>
        /// <returns>True if the world position corresponds to a real voxel, or false otherwise.</returns>
        public bool GetNeighbors(Vector3 worldPosition, List<Vector3> succ, List<Voxel> toReturn)
        {
            toReturn.Clear();
            VoxelChunk chunk = GetVoxelChunkAtWorldLocation(worldPosition);

            if (chunk == null) return false;

            Vector3 grid = chunk.WorldToGrid(worldPosition);

            chunk.GetNeighborsSuccessors(succ, (int) grid.X, (int) grid.Y, (int) grid.Z, toReturn);
            return true;
        }


        /// <summary>
        /// Gets the first visible voxel hit by a ray emenating out of the mouse position.
        /// </summary>
        /// <param name="mouse">The mouse.</param>
        /// <param name="camera">The camera.</param>
        /// <param name="viewPort">The view port.</param>
        /// <param name="selectEmpty">if set to <c>true</c>, returns the empty voxel on the nearest face of the 
        /// selected voxel to the camera. This allows for placing voxels and objects *on* surfaces rather than
        /// *in them.</param>
        /// <returns>The voxel hit by the mouse ray if it exists, or null otherwise.</returns>
        public Voxel GetFirstVisibleBlockHitByMouse(MouseState mouse, Camera camera, Viewport viewPort,
            bool selectEmpty = false)
        {
            Voxel vox = GetFirstVisibleBlockHitByScreenCoord(mouse.X, mouse.Y, camera, viewPort, 150.0f, false,
                selectEmpty);
            return vox;
        }

        /// <summary>
        /// Gets the first visible voxel hit by a ray emenating out of a position on the screen.
        /// </summary>
        /// <param name="x">The x position on the screen (pixels)</param>
        /// <param name="y">The y position on the screen (pixels)</param>
        /// <param name="camera">The camera.</param>
        /// <param name="viewPort">The view port.</param>
        /// <param name="dist">The distance to raycast.</param>
        /// <param name="draw">if set to <c>true</c> , draws debug data.</param>
        /// <param name="selectEmpty">if set to <c>true</c>eturns the empty voxel on the nearest face of the 
        /// selected voxel to the camera. This allows for placing voxels and objects *on* surfaces rather than
        /// *in them </param>
        /// <returns></returns>
        public Voxel GetFirstVisibleBlockHitByScreenCoord(int x, int y, Camera camera, Viewport viewPort, float dist,
            bool draw = false, bool selectEmpty = false)
        {
            Vector3 pos1 = viewPort.Unproject(new Vector3(x, y, 0), camera.ProjectionMatrix, camera.ViewMatrix,
                Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(x, y, 1), camera.ProjectionMatrix, camera.ViewMatrix,
                Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);
            Voxel vox = GetFirstVisibleBlockHitByRay(pos1, pos1 + dir*dist, draw, selectEmpty);

            return vox;
        }

        /// <summary>
        /// Checks if a solid voxel exists between rayStart and rayEnd. This is used for visibility testing.
        /// </summary>
        /// <param name="rayStart">The ray start.</param>
        /// <param name="rayEnd">The ray end.</param>
        /// <returns>True if a solid voxel exists in the ray between two lines, or false otherwise.</returns>
        public bool CheckRaySolid(Vector3 rayStart, Vector3 rayEnd)
        {
            var atPos = new Voxel();
            foreach (Point3 coord in MathFunctions.RasterizeLine(rayStart, rayEnd))
            {
                var pos = new Vector3(coord.X, coord.Y, coord.Z);

                bool success = GetNonNullVoxelAtWorldLocationCheckFirst(null, pos, ref atPos);

                if (success && !atPos.IsEmpty)
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Checks to see if there exists a voxel which is both solid and visible between the ray start and end.
        /// This is used to see if something is visible to the player.
        /// </summary>
        /// <param name="rayStart">The ray start.</param>
        /// <param name="rayEnd">The ray end.</param>
        /// <returns>True if there exists a Voxel that is both solid and visible along the ray.</returns>
        public bool CheckOcclusionRay(Vector3 rayStart, Vector3 rayEnd)
        {
            var atPos = new Voxel();
            foreach (Point3 coord in MathFunctions.RasterizeLine(rayStart, rayEnd))
            {
                var pos = new Vector3(coord.X, coord.Y, coord.Z);

                bool success = GetNonNullVoxelAtWorldLocationCheckFirst(null, pos, ref atPos);

                if (success && atPos.IsVisible)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the first solid voxel beneath some point in the world.
        /// </summary>
        /// <param name="rayStart">The ray start.</param>
        /// <param name="under">Returns the voxel beneath the position.</param>
        /// <param name="considerWater">if set to <c>true</c>, a voxel with water is considered "solid"</param>
        /// <returns>True if such a voxel exists, or false otherwise.</returns>
        public bool GetFirstVoxelUnder(Vector3 rayStart, ref Voxel under, bool considerWater = false)
        {
            VoxelChunk startChunk = GetVoxelChunkAtWorldLocation(rayStart);

            if (startChunk == null)
            {
                return false;
            }

            var point = new Point3(startChunk.WorldToGrid(rayStart));

            for (int y = point.Y; y >= 0; y--)
            {
                int index = startChunk.Data.IndexAt(point.X, y, point.Z);

                if (startChunk.Data.Types[index] != 0 || (considerWater && startChunk.Data.Water[index].WaterLevel > 0))
                {
                    under.Chunk = startChunk;
                    under.GridPosition = new Vector3(point.X, y, point.Z);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the first visible and solid voxel intersecting the ray between two points.
        /// </summary>
        /// <param name="rayStart">The start of the line segment.</param>
        /// <param name="rayEnd">The end of the line segment.</param>
        /// <returns>The first visible and solid voxel intersecting the line segment if it exists, or false otherwise.</returns>
        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd)
        {
            return GetFirstVisibleBlockHitByRay(rayStart, rayEnd, null, false);
        }

        /// <summary>
        /// Gets the first visible and solid voxel intersecting the ray between two points.
        /// </summary>
        /// <param name="rayStart">The ray start.</param>
        /// <param name="rayEnd">The ray end.</param>
        /// <param name="draw">if set to <c>true</c> draws debug data.</param>
        /// <param name="selectEmpty">if set to <c>true</c> instead returns the nearest empty voxel "on" the one selected.</param>
        /// <returns>The first visible voxel intersecting the line segment between start and end.</returns>
        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd, bool draw, bool selectEmpty)
        {
            return GetFirstVisibleBlockHitByRay(rayStart, rayEnd, null, draw, selectEmpty);
        }


        /// <summary>
        /// Gets the first visible and solid voxel intersecting the ray between two points.
        /// </summary>
        /// <param name="rayStart">The ray start.</param>
        /// <param name="rayEnd">The ray end.</param>
        /// <param name="ignore">Ignores the specified voxel (skips over it)</param>
        /// <param name="draw">if set to <c>true</c> draws debug data.</param>
        /// <param name="selectEmpty">if set to <c>true</c> instead returns the nearest empty voxel "on" the one selected.</param>
        /// <returns>The first visible voxel intersecting the line segment between start and end.</returns>
        public Voxel GetFirstVisibleBlockHitByRay(Vector3 rayStart, Vector3 rayEnd, Voxel ignore, bool draw,
            bool selectEmpty = false)
        {
            Vector3 delta = rayEnd - rayStart;
            float length = delta.Length();
            delta.Normalize();
            var atPos = new Voxel();
            var prev = new Voxel();
            foreach (Point3 coord in MathFunctions.RasterizeLine(rayStart, rayEnd))
            {
                var pos = new Vector3(coord.X, coord.Y, coord.Z);

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

        /// <summary>
        /// Adds the chunk to the map.
        /// </summary>
        /// <param name="chunk">The chunk to add.</param>
        /// <returns>True if the chunk could be added, or false if it already exists.</returns>
        public bool AddChunk(VoxelChunk chunk)
        {
            if (ChunkMap.Count < MaxChunks && !ChunkMap.ContainsKey(chunk.ID))
            {
                ChunkMap[chunk.ID] = chunk;
                return true;
            }
            if (ChunkMap.ContainsKey(chunk.ID))
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// Removes the chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        public void RemoveChunk(VoxelChunk chunk)
        {
            VoxelChunk removed = null;
            while (!ChunkMap.TryRemove(chunk.ID, out removed))
            {
                Thread.Sleep(10);
            }

            var locatables = new HashSet<Body>();

            chunkManager.Components.CollisionManager.GetObjectsIntersecting(chunk.GetBoundingBox(), locatables,
                CollisionManager.CollisionType.Static | CollisionManager.CollisionType.Dynamic);

            foreach (Body component in locatables)
            {
                component.IsDead = true;
            }

            chunk.Destroy(chunkManager.Graphics);
        }

        /// <summary>
        /// Gets chunks that are adjacent to (8-connected in x and z) the specified chunk.
        /// </summary>
        /// <param name="chunk">The chunk.</param>
        /// <returns>A list of chunks adjacent to the specified chunk.</returns>
        public List<VoxelChunk> GetAdjacentChunks(VoxelChunk chunk)
        {
            var toReturn = new List<VoxelChunk>();
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dz = -1; dz < 2; dz++)
                {
                    if (dx != 0 || dz != 0)
                    {
                        var key = new Point3(chunk.ID.X + dx, 0, chunk.ID.Z + dz);

                        if (ChunkMap.ContainsKey(key))
                        {
                            toReturn.Add(ChunkMap[key]);
                        }
                    }
                }
            }

            return toReturn;
        }

        /// <summary>
        /// Recomputes the neighbors of this chunk (8 connected in x and z) and caches them.
        /// </summary>
        public void RecomputeNeighbors()
        {
            foreach (VoxelChunk chunk in ChunkMap.Select(chunks => chunks.Value))
            {
                chunk.Neighbors.Clear();
            }

            foreach (var chunks in ChunkMap)
            {
                VoxelChunk chunk = chunks.Value;
                List<VoxelChunk> adjacents = GetAdjacentChunks(chunk);
                foreach (VoxelChunk c in adjacents)
                {
                    if (!c.Neighbors.ContainsKey(chunk.ID) && chunk != c)
                    {
                        c.Neighbors[chunk.ID] = (chunk);
                    }
                    chunk.Neighbors[c.ID] = c;
                }
            }
        }


        /// <summary>
        /// Given some floating point location relative to the world, rounds it so that
        /// it contains integer locations where each dimension is an index of a chunk.
        /// </summary>
        /// <param name="location">The location in the world.</param>
        /// <returns>the location rounded to the nearest chunk position (integer coordinates)</returns>
        public Vector3 RoundToChunkCoords(Vector3 location)
        {
            int x = MathFunctions.FloorInt(location.X*InvCSX);
            int y = MathFunctions.FloorInt(location.Y*InvCSY);
            int z = MathFunctions.FloorInt(location.Z*InvCSZ);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Gets the voxel chunk at world location.
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <returns>The chunk at the world location if it exists, or null otherwise.</returns>
        public VoxelChunk GetVoxelChunkAtWorldLocation(Vector3 worldLocation)
        {
            Point3 id = GetChunkID(worldLocation);

            if (ChunkMap.ContainsKey(id))
            {
                return ChunkMap[id];
            }


            return null;
        }

        /// <summary>
        /// Gets the voxel at the specified world location.
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <param name="voxel">The voxel at that location.</param>
        /// <returns>true if there is a voxel at that location, or false otherwise.</returns>
        public bool GetVoxel(Vector3 worldLocation, ref Voxel voxel)
        {
            return GetVoxel(null, worldLocation, ref voxel);
        }

        /// <summary>
        /// Gets the voxel at the specified world location.
        /// </summary>
        /// <param name="checkFirst">If we already know the voxel is in this chunk, check it first.</param>
        /// <param name="worldLocation">The world location.</param>
        /// <param name="newReference">The new reference to a voxel at that location.</param>
        /// <returns>True if a voxel exists at that location, and false otherwise.</returns>
        public bool GetVoxel(VoxelChunk checkFirst, Vector3 worldLocation, ref Voxel newReference)
        {
            if (checkFirst != null)
            {
                if (checkFirst.IsWorldLocationValid(worldLocation))
                {
                    if (!checkFirst.GetVoxelAtWorldLocation(worldLocation, ref newReference))
                    {
                        return false;
                    }

                    Vector3 grid = checkFirst.WorldToGrid(worldLocation);
                    newReference.Chunk = checkFirst;
                    newReference.GridPosition = new Vector3((int)grid.X, (int)grid.Y, (int)grid.Z);

                    return true;
                }
            }

            VoxelChunk chunk = GetVoxelChunkAtWorldLocation(worldLocation);

            if (chunk == null)
            {
                return false;
            }
            return chunk.GetVoxelAtWorldLocation(worldLocation, ref newReference);
        }

        /// <summary>
        /// Gets the chunk identifier (x, y, z) coordinate from a chunk origin (floating point x, y, z)
        /// </summary>
        /// <param name="origin">The origin (floating point x, y, z).</param>
        /// <returns>Chunk identifier (integer x, y, z) of the chunk there.</returns>
        public Point3 GetChunkID(Vector3 origin)
        {
            origin = RoundToChunkCoords(origin);
            return new Point3(MathFunctions.FloorInt(origin.X), MathFunctions.FloorInt(origin.Y),
                MathFunctions.FloorInt(origin.Z));
        }

        /// <summary>
        ///     Given a world location, returns the voxel at that location if it exists
        ///     Otherwise returns null.
        /// </summary>
        /// <param Name="worldLocation">A floating point vector location in the world space</param>
        /// <param Name="depth">unused</param>
        /// <returns>The voxel at that location (as a list)</returns>
        public bool GetNonNullVoxelAtWorldLocation(Vector3 worldLocation, ref Voxel voxel)
        {
            return GetNonNullVoxelAtWorldLocationCheckFirst(null, worldLocation, ref voxel);
        }

        /// <summary>
        /// Given a location in the world
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns></returns>
        public float GetFilledVoxelGridHeightAt(float x, float y, float z)
        {
            VoxelChunk chunk = GetVoxelChunkAtWorldLocation(new Vector3(x, y, z));

            if (chunk != null)
            {
                return chunk.GetFilledVoxelGridHeightAt((int) (x - chunk.Origin.X), (int) (y - chunk.Origin.Y),
                    (int) (z - chunk.Origin.Z));
            }
            return -1;
        }

        /// <summary>
        ///     TODO: Get rid of the recursion
        ///     Recursive function which gets all the voxels at a position in the world, assuming the voxel is in a given chunk
        /// 
        ///     TODO(mklingen): I wrote this so long ago I'm not really sure why it exits when GetVoxel exists. I think this 
        ///     comes from a time when multiple chunks could intersect?
        /// </summary>
        /// <param Name="checkFirst">The voxel chunk to check first</param>
        /// <param Name="worldLocation">The point in the world to check</param>
        /// <param Name="toReturn">A list of voxels to get</param>
        /// <param Name="depth">The depth of the recursion</param>
        public bool GetNonNullVoxelAtWorldLocationCheckFirst(VoxelChunk checkFirst, Vector3 worldLocation,
            ref Voxel toReturn)
        {
            if (checkFirst != null)
            {
                if (!checkFirst.IsWorldLocationValid(worldLocation))
                {
                    return GetNonNullVoxelAtWorldLocation(worldLocation, ref toReturn);
                }

                bool success = checkFirst.GetVoxelAtWorldLocation(worldLocation, ref toReturn);

                if (success && !toReturn.IsEmpty)
                {
                    return true;
                }
                return GetNonNullVoxelAtWorldLocation(worldLocation, ref toReturn);
            }

            VoxelChunk chunk = GetVoxelChunkAtWorldLocation(worldLocation);

            if (chunk == null)
            {
                return false;
            }

            if (!chunk.IsWorldLocationValid(worldLocation))
            {
                return false;
            }

            if (chunk.GetVoxelAtWorldLocation(worldLocation, ref toReturn) && !toReturn.IsEmpty)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets all liquid primitive (vertex buffers for water, lava etc.) associated with the chunk.
        /// </summary>
        /// <returns>A list of liquid primitives associated with the chunk.</returns>
        public List<LiquidPrimitive> GetAllLiquidPrimitives()
        {
            var toReturn = new List<LiquidPrimitive>();

            foreach (VoxelChunk chunk in ChunkMap.Select(chunks => chunks.Value))
            {
                toReturn.AddRange(chunk.Liquids.Values);
            }

            return toReturn;
        }

        /// <summary>
        /// Determines if a water cell exists at some world location.
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <returns>True if a water cell exists at that location, and false otherwise.</returns>
        public bool DoesWaterCellExist(Vector3 worldLocation)
        {
            VoxelChunk chunkAtLocation = GetVoxelChunkAtWorldLocation(worldLocation);
            return chunkAtLocation != null && chunkAtLocation.IsWorldLocationValid(worldLocation);
        }

        /// <summary>
        /// Gets the water cell at the specified location.
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <returns>A water cell, if it exists, and an empty cell otherwise.</returns>
        public WaterCell GetWaterCellAtLocation(Vector3 worldLocation)
        {
            VoxelChunk chunkAtLocation = GetVoxelChunkAtWorldLocation(worldLocation);

            if (chunkAtLocation == null)
            {
                return new WaterCell();
            }

            Vector3 gridPos = chunkAtLocation.WorldToGrid(worldLocation);
            return
                chunkAtLocation.Data.Water[
                    chunkAtLocation.Data.IndexAt((int) gridPos.X, (int) gridPos.Y, (int) gridPos.Z)];
        }

        /// <summary>
        /// Saves all chunks to a file.
        /// </summary>
        /// <param name="directory">The directory to save to.</param>
        /// <param name="compress">if set to <c>true</c> GZIP compress chunk data.</param>
        /// <returns>true if saving was successful, false otherwise</returns>
        public bool SaveAllChunks(string directory, bool compress)
        {
            foreach (var pair in ChunkMap)
            {
                var chunkFile = new ChunkFile(pair.Value);

                string fileName = directory + Path.DirectorySeparatorChar + pair.Key.X + "_" + pair.Key.Y + "_" +
                                  pair.Key.Z;

                if (compress)
                {
                    fileName += ".zch";
                }
                else
                {
                    fileName += ".jch";
                }

                if (!chunkFile.WriteFile(fileName, compress, true))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Loads chunks from a file.
        /// </summary>
        /// <param name="gameFile">The game file.</param>
        /// <param name="loadingMessage">The loading message to display while loading</param>
        public void LoadFromFile(GameFile gameFile, ref string loadingMessage)
        {
            foreach (VoxelChunk chunk in gameFile.Data.ChunkData.Select(file => file.ToChunk(chunkManager)))
            {
                AddChunk(chunk);
            }
            chunkManager.UpdateBounds();
            chunkManager.UpdateRebuildList();
            chunkManager.CreateGraphics(ref loadingMessage, this);
        }
    }
}