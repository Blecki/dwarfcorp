using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DwarfCorp.Generation
{
    public static partial class Generator
    { 
        public class BalloonPortVoxelSets
        {
            public List<VoxelHandle> StockpileVoxels;
        }

        public static BalloonPortVoxelSets GenerateBalloonPort(ChunkManager chunkManager, float x, float z, int size, ChunkGeneratorSettings Settings)
        {
            x = MathHelper.Clamp(x, size + 1, (Settings.WorldSizeInChunks.X * VoxelConstants.ChunkSizeX) - size - 1);
            z = MathHelper.Clamp(z, size + 1, (Settings.WorldSizeInChunks.Z * VoxelConstants.ChunkSizeY) - size - 1);

            var centerCoordinate = GlobalVoxelCoordinate.FromVector3(new Vector3(x, (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 1, z));

            var stockpileYPosition = (int)Math.Ceiling(GetAverageHeight(centerCoordinate.X - size, centerCoordinate.Z - size, size * 2 + 1, size * 2 + 1, Settings));

            // Next, create the balloon port by deciding which voxels to fill.
            var balloonPortDesignations = new List<VoxelHandle>();

            for (int dx = -size; dx <= size; dx++)
                for (int dz = -size; dz <= size; dz++)
                {
                    var worldPos = new Vector3(centerCoordinate.X + dx, centerCoordinate.Y, centerCoordinate.Z + dz);

                    var baseVoxel = VoxelHelpers.FindFirstVoxelBelow(chunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(worldPos)));

                    if (!baseVoxel.IsValid)
                        continue;

                    var groundYPosition = baseVoxel.Coordinate.Y + 1;

                    for (int y = stockpileYPosition; y < groundYPosition && y < chunkManager.World.WorldSizeInVoxels.Y; y++)
                    {
                        var v = chunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(baseVoxel.Coordinate.X, y, baseVoxel.Coordinate.Z));
                        v.RawSetType(Library.EmptyVoxelType);
                        v.RawSetIsExplored();
                        v.QuickSetLiquid(0, 0);
                    }

                    if (stockpileYPosition < groundYPosition)
                        groundYPosition = stockpileYPosition;

                    var ladderOffset = Vector3.Zero;
                    var hasLadder = false;

                    if (dx == size && dz == 0)
                    {
                        hasLadder = true;
                        ladderOffset = Vector3.UnitX;
                    }
                    else if (dz == size & dx == 0)
                    {
                        hasLadder = true;
                        ladderOffset = Vector3.UnitZ;
                    }
                    else if (dx == -size && dz == 0)
                    {
                        hasLadder = true;
                        ladderOffset = -Vector3.UnitX;
                    }
                    else if (dz == -size && dz == 0)
                    {
                        hasLadder = true;
                        ladderOffset = -Vector3.UnitZ;
                    }

                    // Fill from the top height down to the bottom.

                    for (int y = Math.Max(0, groundYPosition - 1); y < stockpileYPosition && y < chunkManager.World.WorldSizeInVoxels.Y; y++)
                    {
                        var v = chunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(baseVoxel.Coordinate.X, y, baseVoxel.Coordinate.Z));
                        if (!v.IsValid) throw new InvalidProgramException("Voxel was invalid while creating a new game's initial zones. This should not happen.");

                        if (y == stockpileYPosition - 1)
                        {
                            v.RawSetType(Library.GetVoxelType("Plank"));
                            v.RawSetIsExplored();
                            balloonPortDesignations.Add(v);
                        }
                        else
                            v.RawSetType(Library.GetVoxelType("Scaffold"));

                        v.IsPlayerBuilt = true;
                        v.QuickSetLiquid(0, 0);
                        v.Sunlight = false;

                        if (hasLadder)
                        {
                            var ladderPos = new Vector3(worldPos.X, y, worldPos.Z) + ladderOffset + Vector3.One * 0.5f;
                            var ladderVox = chunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(ladderPos));
                            if (ladderVox.IsValid && ladderVox.IsEmpty)
                            {
                                var ladder = EntityFactory.CreateEntity<WoodenLadder>("Wooden Ladder", ladderPos);
                                Settings.World.PlayerFaction.OwnedObjects.Add(ladder);
                                ladder.Tags.Add("Moveable");
                                ladder.Tags.Add("Deconstructable");
                            }
                        }
                    }

                    CastSunlightColumn(baseVoxel.Coordinate.X, baseVoxel.Coordinate.Z, Settings);
                }

            return new BalloonPortVoxelSets
            {
                StockpileVoxels = balloonPortDesignations
            };
        }
    }
}
