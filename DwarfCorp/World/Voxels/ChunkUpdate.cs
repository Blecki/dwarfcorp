using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Collections.Concurrent;
using System.Threading;

namespace DwarfCorp
{
    public class ChunkUpdate
    {
        private static int CurrentUpdateChunk = 0;

        public static void RunUpdate(ChunkManager Chunks)
        {
            if (CurrentUpdateChunk < 0 || CurrentUpdateChunk >= Chunks.ChunkMap.Length)
            {
                return;
            }

            var chunk = Chunks.ChunkMap[CurrentUpdateChunk];
            CurrentUpdateChunk += 1;
            if (CurrentUpdateChunk >= Chunks.ChunkMap.Length)
                CurrentUpdateChunk = 0;

            UpdateChunk(chunk);
        }

        private static void UpdateChunk(VoxelChunk chunk)
        {
            var addGrassToThese = new List<Tuple<VoxelHandle, byte>>();
            for (var y = 0; y < VoxelConstants.ChunkSizeY; ++y)
            {
                // Skip empty slices.
                if (chunk.Data.VoxelsPresentInSlice[y] == 0) continue;

                for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
                    for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                    {
                        var voxel = VoxelHandle.UnsafeCreateLocalHandle(chunk, new LocalVoxelCoordinate(x, y, z));

                        // Allow grass to decay
                        if (voxel.GrassType != 0)
                        {
                            var grass = Library.GetGrassType(voxel.GrassType);

                            if (grass.NeedsSunlight && !voxel.Sunlight)
                                voxel.GrassType = 0;
                            else if (grass.Decay)
                            {                                
                                if (voxel.GrassDecay == 0)
                                {
                                    var newDecal = Library.GetGrassType(grass.BecomeWhenDecays);
                                    if (newDecal != null)
                                        voxel.GrassType = newDecal.ID;
                                    else
                                        voxel.GrassType = 0;
                                }
                                else
                                    voxel.GrassDecay -= 1;
                            } 
                        }
//#if false
                        else if (voxel.Type.GrassSpreadsHere)
                        {
                            // Spread grass onto this tile - but only from the same biome.

                            // Don't spread if there's an entity here.
                             var entityPresent = chunk.Manager.World.EnumerateIntersectingObjects(
                                new BoundingBox(voxel.WorldPosition + new Vector3(0.1f, 1.1f, 0.1f), voxel.WorldPosition + new Vector3(0.9f, 1.9f, 0.9f)),
                                CollisionType.Static).Any();
                            if (entityPresent) continue;

                            var biome = chunk.Manager.World.Settings.Overworld.GetBiomeAt(voxel.Coordinate.ToVector3(), chunk.Manager.World.Settings.InstanceSettings.Origin);

                            var grassyNeighbors = VoxelHelpers.EnumerateManhattanNeighbors2D(voxel.Coordinate)
                                .Select(c => new VoxelHandle(voxel.Chunk.Manager, c))
                                .Where(v => v.IsValid && v.GrassType != 0)
                                .Where(v => Library.GetGrassType(v.GrassType).Spreads)
                                .Where(v => biome == chunk.Manager.World.Settings.Overworld.GetBiomeAt(v.Coordinate.ToVector3(), chunk.Manager.World.Settings.InstanceSettings.Origin))
                                .ToList();

                            if (grassyNeighbors.Count > 0)
                                if (MathFunctions.RandEvent(0.1f))
                                    addGrassToThese.Add(Tuple.Create(voxel, grassyNeighbors[MathFunctions.RandInt(0, grassyNeighbors.Count)].GrassType));
                        }
//#endif
                    }
            }

            foreach (var v in addGrassToThese)
            {
                var l = v.Item1;
                var grassType = Library.GetGrassType(v.Item2);
                if (grassType.NeedsSunlight && !l.Sunlight)
                    continue;
                l.GrassType = v.Item2;
            }
        }
    }
}
