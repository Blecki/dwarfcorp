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
            public List<VoxelHandle> TreasuryVoxels;
        }

        public static BalloonPortVoxelSets GenerateBalloonPort(ChunkManager chunkManager, float x, float z, int size, GeneratorSettings Settings)
        {
            var centerCoordinate = GlobalVoxelCoordinate.FromVector3(new Vector3(x, (Settings.WorldSizeInChunks.Y * VoxelConstants.ChunkSizeY) - 1, z));

            var averageHeight = 0;

            if (Settings.OverworldSettings.StartUnderground)
            {
                var accumulator = 0;
                var count = 0;
                List<string> illegalTypes = new List<string>() { "Sand", "Dirt", "DarkDirt", "Ice" };
                for (var offsetX = -size; offsetX <= size; ++offsetX)
                {
                    for (var offsetY = -size; offsetY <= size; ++offsetY)
                    {
                        var topVoxel = VoxelHelpers.FindFirstVoxelBelow(chunkManager.CreateVoxelHandle(centerCoordinate + new GlobalVoxelOffset(offsetX, 0, offsetY)));

                        if (topVoxel.Coordinate.Y > 0)
                        {
                            var vox = topVoxel;
                            for (int dy = topVoxel.Coordinate.Y; dy > 0; dy--)
                            {
                                vox = chunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(topVoxel.Coordinate.X, dy, topVoxel.Coordinate.Z));
                                if (vox.IsValid && !vox.IsEmpty && !illegalTypes.Contains(vox.Type.Name))
                                {
                                    break;
                                }
                            }
                            accumulator += vox.Coordinate.Y + 1;
                            count += 1;
                        }

                    }
                }
                averageHeight = Math.Max((int)Math.Round(((float)accumulator / (float)count)) - 5, 0);
            }
            else
                averageHeight = (int)GetAverageHeight(centerCoordinate.X - size, centerCoordinate.Y - size, size * 2 + 1, size * 2 + 1, Settings);


            // Next, create the balloon port by deciding which voxels to fill.
            var balloonPortDesignations = new List<VoxelHandle>();
            var treasuryDesignations = new List<VoxelHandle>();
            for (int dx = -size; dx <= size; dx++)
            {
                for (int dz = -size; dz <= size; dz++)
                {
                    var worldPos = new Vector3(centerCoordinate.X + dx, centerCoordinate.Y, centerCoordinate.Z + dz);

                    var baseVoxel = VoxelHelpers.FindFirstVoxelBelow(chunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(worldPos)));

                    if (!baseVoxel.IsValid)
                        continue;

                    var h = baseVoxel.Coordinate.Y + 1;

                    for (int y = averageHeight; y < (Settings.OverworldSettings.StartUnderground ? averageHeight + 2 : h) && y < chunkManager.World.WorldSizeInVoxels.Y; y++)
                    {
                        var v = chunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(baseVoxel.Coordinate.X, y, baseVoxel.Coordinate.Z));
                        v.RawSetType(VoxelLibrary.GetVoxelType(0));
                        v.RawSetIsExplored();
                        v.QuickSetLiquid(LiquidType.None, 0);
                    }

                    if (averageHeight < h)
                    {
                        h = averageHeight;
                    }

                    bool isPosX = (dx == size && dz == 0);
                    bool isPosZ = (dz == size & dx == 0);
                    bool isNegX = (dx == -size && dz == 0);
                    bool isNegZ = (dz == -size && dz == 0);
                    bool isSide = (isPosX || isNegX || isPosZ || isNegZ);

                    Vector3 offset = Vector3.Zero;

                    if (isSide)
                    {
                        if (isPosX)
                        {
                            offset = Vector3.UnitX;
                        }
                        else if (isPosZ)
                        {
                            offset = Vector3.UnitZ;
                        }
                        else if (isNegX)
                        {
                            offset = -Vector3.UnitX;
                        }
                        else if (isNegZ)
                        {
                            offset = -Vector3.UnitZ;
                        }
                    }

                    // Fill from the top height down to the bottom.
                    for (int y = Math.Max(0, h - 1); y < averageHeight && y < chunkManager.World.WorldSizeInVoxels.Y; y++)
                    {
                        var v = chunkManager.CreateVoxelHandle(new GlobalVoxelCoordinate(baseVoxel.Coordinate.X, y, baseVoxel.Coordinate.Z));
                        if (!v.IsValid) throw new InvalidProgramException("Voxel was invalid while creating a new game's initial zones. This should not happen.");

                        v.RawSetType(VoxelLibrary.GetVoxelType("Scaffold"));
                        v.IsPlayerBuilt = true;
                        v.QuickSetLiquid(LiquidType.None, 0);
                        v.Sunlight = false;

                        if (y == averageHeight - 1)
                        {
                            v.RawSetIsExplored();

                            if (dz >= 0)
                            {
                                balloonPortDesignations.Add(v);
                            }
                            else
                            {
                                treasuryDesignations.Add(v);
                            }
                        }

                        if (isSide)
                        {
                            var ladderPos = new Vector3(worldPos.X, y, worldPos.Z) + offset + Vector3.One * 0.5f;
                            var ladderVox = chunkManager.CreateVoxelHandle(GlobalVoxelCoordinate.FromVector3(ladderPos));
                            if (ladderVox.IsValid && ladderVox.IsEmpty)
                            {
                                // Todo: WTF why is this static?????
                                WorldManager.DoLazy(new Action(() =>
                                {
                                    var ladder = EntityFactory.CreateEntity<Ladder>("Ladder", ladderPos);
                                    Settings.World.Master.Faction.OwnedObjects.Add(ladder);
                                    ladder.Tags.Add("Moveable");
                                    ladder.Tags.Add("Deconstructable");
                                }));
                            }

                        }
                    }

                    CastSunlightColumn(baseVoxel.Coordinate.X, baseVoxel.Coordinate.Z, Settings);
                }
            }

            return new BalloonPortVoxelSets
            {
                StockpileVoxels = balloonPortDesignations,
                TreasuryVoxels = treasuryDesignations
            };
        }
    }
}
