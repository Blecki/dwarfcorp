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
        private static Dictionary<String, System.Reflection.MethodInfo> VoxelUpdateHooks;

        private static void DiscoverHooks()
        {
            VoxelUpdateHooks = new Dictionary<string, System.Reflection.MethodInfo>();

            foreach (var method in AssetManager.EnumerateModHooks(typeof(VoxelUpdateHookAttribute), typeof(void), new Type[] { typeof(VoxelHandle), typeof(WorldManager) }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is VoxelUpdateHookAttribute) as VoxelUpdateHookAttribute;
                if (attribute == null) continue;
                VoxelUpdateHooks[attribute.Name] = method;
            }
        }

        private static void InvokeVoxelUpdateHook(String Hook, VoxelHandle Voxel, WorldManager World)
        {
            if (!VoxelUpdateHooks.ContainsKey(Hook))
                return;

            try
            {
                VoxelUpdateHooks[Hook].Invoke(null, new Object[] { Voxel, World });
            }
            catch (Exception e)
            {

            }
        }

        public static void ChunkUpdateThread(ChunkManager Chunks)
        {
            DiscoverHooks();
            var timeOfLastChunk = 0.0;
            while (!Chunks.ExitThreads && !DwarfGame.ExitGame)
            {
                if (!DwarfTime.LastTimeX.IsPaused)
                {
                    if (timeOfLastChunk < (DwarfTime.LastTimeX.TotalGameTime.TotalSeconds - GameSettings.Current.ChunkUpdateTime))
                    {
                        ChunkUpdate.RunUpdate(Chunks);
                        timeOfLastChunk = DwarfTime.LastTimeX.TotalGameTime.TotalSeconds;
                    }
                }
                else
                    Thread.Sleep(100);
            }
        }

        private static void RunUpdate(ChunkManager Chunks)
        {
            if (CurrentUpdateChunk < 0 || CurrentUpdateChunk >= Chunks.ChunkMap.Length)
                CurrentUpdateChunk = 0;

            var chunk = Chunks.ChunkMap[CurrentUpdateChunk];
            UpdateChunk(chunk);

            CurrentUpdateChunk += 1;
        }

        private struct GrassLocation
        {
            public VoxelHandle Voxel;
            public byte GrassType;
        }

        private static void UpdateGrass(VoxelChunk Chunk, VoxelHandle Voxel, List<GrassLocation> AddGrassToThese)
        {
            if (Voxel.GrassType != 0)
            {
                var grass = Library.GetGrassType(Voxel.GrassType);

                if (grass.NeedsSunlight && !Voxel.Sunlight)
                    Voxel.GrassType = 0;
                else if (grass.Decay)
                {
                    if (Voxel.GrassDecay == 0)
                    {
                        var newDecal = Library.GetGrassType(grass.BecomeWhenDecays);
                        if (newDecal != null)
                            Voxel.GrassType = newDecal.ID;
                        else
                            Voxel.GrassType = 0;
                    }
                    else
                        Voxel.GrassDecay -= 1;
                }
            }

            else if (Voxel.Type.GrassSpreadsHere)
            {
                // Spread grass onto this tile - but only from the same biome.

                // Don't spread if there's an entity here.
                var entityPresent = Chunk.Manager.World.EnumerateIntersectingRootObjects(
                   new BoundingBox(Voxel.WorldPosition + new Vector3(0.1f, 1.1f, 0.1f), Voxel.WorldPosition + new Vector3(0.9f, 1.9f, 0.9f)),
                   CollisionType.Static).Any();
                if (entityPresent) return;

                // Don't spread if there's a voxel above us.
                var voxelAbove = VoxelHelpers.GetVoxelAbove(Voxel);
                if (voxelAbove.IsValid && !voxelAbove.IsEmpty)
                    return;

                if (Chunk.Manager.World.Overworld.Map.GetBiomeAt(Voxel.Coordinate.ToVector3()).HasValue(out var biome))
                {
                    var grassyNeighbors = VoxelHelpers.EnumerateManhattanNeighbors2D(Voxel.Coordinate)
                        .Select(c => new VoxelHandle(Voxel.Chunk.Manager, c))
                        .Where(v => v.IsValid && v.GrassType != 0)
                        .Where(v => Library.GetGrassType(v.GrassType).Spreads)
                        .Where(v =>
                        {
                            if (Chunk.Manager.World.Overworld.Map.GetBiomeAt(v.Coordinate.ToVector3()).HasValue(out var otherBiome))
                                return biome == otherBiome;
                            return false;
                        })
                        .ToList();

                    if (grassyNeighbors.Count > 0)
                        if (MathFunctions.RandEvent(0.1f))
                            AddGrassToThese.Add(new GrassLocation { Voxel = Voxel, GrassType = grassyNeighbors[MathFunctions.RandInt(0, grassyNeighbors.Count)].GrassType });
                }
            }
        }

        private static void PlaceGrass(List<GrassLocation> GrassLocations)
        {
            foreach (var location in GrassLocations)
            {
                var grassType = Library.GetGrassType(location.GrassType); // Todo: Library func should return a maybenull.
                if (grassType.NeedsSunlight && !location.Voxel.Sunlight)
                    continue;
                var handle = location.Voxel;
                handle.GrassType = location.GrassType;
            }
        }

        private static void UpdateChunk(VoxelChunk chunk)
        {
            var addGrassToThese = new List<GrassLocation>();

            for (var y = 0; y < VoxelConstants.ChunkSizeY; ++y)
            {
                // Skip empty slices.
                if (chunk.Data.VoxelsPresentInSlice[y] == 0) continue;

                var updateDither = chunk.UpdateDitherPattern;
                for (var x = 0; x < VoxelConstants.ChunkSizeX; ++x)
                {
                    for (var z = 0; z < VoxelConstants.ChunkSizeZ; ++z)
                    {
                        updateDither = (updateDither + 1) % 2;
                        if (updateDither != 1) continue;

                        var voxel = VoxelHandle.UnsafeCreateLocalHandle(chunk, new LocalVoxelCoordinate(x, y, z));

                        if (!String.IsNullOrEmpty(voxel.Type.UpdateHook))
                            InvokeVoxelUpdateHook(voxel.Type.UpdateHook, voxel, chunk.Manager.World);

                        UpdateGrass(chunk, voxel, addGrassToThese);
                    }
                    updateDither = (updateDither + 1) % 2;
                }
            }

            chunk.UpdateDitherPattern = (chunk.UpdateDitherPattern + 1) % 2;

            PlaceGrass(addGrassToThese);
        }
    }
}
